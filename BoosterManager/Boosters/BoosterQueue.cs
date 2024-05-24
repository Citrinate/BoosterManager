using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Collections;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;

namespace BoosterManager {
	internal sealed class BoosterQueue {
		private readonly Bot Bot;
		private readonly Timer Timer;
		private readonly ConcurrentHashSet<Booster> Boosters = new(new BoosterComparer());
		private uint GooAmount = 0;
		private uint TradableGooAmount = 0;
		private uint UntradableGooAmount = 0;
		internal uint AvailableGems => BoosterHandler.AllowCraftUntradableBoosters ? GooAmount : TradableGooAmount;
		internal event Action<Dictionary<uint, Steam.BoosterInfo>>? OnBoosterInfosUpdated;
		internal event Action<Booster, BoosterDequeueReason>? OnBoosterRemoved;
		private const int MinDelayBetweenBoosters = 5; // Minimum delay, in seconds, between booster crafts
		private const float BoosterInfosUpdateBackOffMultiplierDefault = 1.0F;
		private const float BoosterInfosUpdateBackOffMultiplierStep = 0.5F;
		private const int BoosterInfosUpdateBackOffMinMinutes = 1;
		private const int BoosterInfosUpdateBackOffMaxMinutes = 15;
		private float BoosterInfosUpdateBackOffMultiplier = BoosterInfosUpdateBackOffMultiplierDefault;
		private SemaphoreSlim RunSemaphore = new SemaphoreSlim(1, 1);

		internal BoosterQueue(Bot bot) {
			Bot = bot;
			Timer = new Timer(
				async e => await Run().ConfigureAwait(false),
				null, 
				Timeout.Infinite, 
				Timeout.Infinite
			);
		}

		internal void Start() {
			Utilities.InBackground(async() => await Run().ConfigureAwait(false));
		}

		private async Task Run() {
			await RunSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				if (!Bot.IsConnectedAndLoggedOn) {
					UpdateTimer(DateTime.Now.AddSeconds(1));

					return;
				}

				// Reload the booster creator page
				if (!await UpdateBoosterInfos().ConfigureAwait(false)) {
					// Reload failed, try again later
					Bot.ArchiLogger.LogGenericError(Strings.BoosterInfoUpdateFailed);
					UpdateTimer(DateTime.Now.AddMinutes(Math.Min(BoosterInfosUpdateBackOffMaxMinutes, BoosterInfosUpdateBackOffMinMinutes * BoosterInfosUpdateBackOffMultiplier)));
					BoosterInfosUpdateBackOffMultiplier += BoosterInfosUpdateBackOffMultiplierStep;

					return;
				}

				Booster? booster = GetNextCraftableBooster();
				if (booster == null) {
					// Booster queue is empty
					BoosterInfosUpdateBackOffMultiplier = BoosterInfosUpdateBackOffMultiplierDefault;

					return;
				}
				
				if (DateTime.Now >= booster.GetAvailableAtTime()) {
					// Attempt to craft the next booster in the queue
					if (booster.Info.Price > AvailableGems) {
						// Not enough gems, wait until we get more gems
						booster.BoosterJob.OnInsufficientGems(booster);
						OnBoosterInfosUpdated += ForceUpdateBoosterInfos;
						UpdateTimer(DateTime.Now.AddMinutes(Math.Min(BoosterInfosUpdateBackOffMaxMinutes, BoosterInfosUpdateBackOffMinMinutes * BoosterInfosUpdateBackOffMultiplier)));
						BoosterInfosUpdateBackOffMultiplier += BoosterInfosUpdateBackOffMultiplierStep;

						return;
					}

					BoosterInfosUpdateBackOffMultiplier = BoosterInfosUpdateBackOffMultiplierDefault;

					if (!await CraftBooster(booster).ConfigureAwait(false)) {
						// Craft failed, decide whether or not to remove this booster from the queue
						Bot.ArchiLogger.LogGenericError(String.Format(Strings.BoosterCreationFailed, booster.GameID));
						VerifyCraftBoosterError(booster);
						UpdateTimer(DateTime.Now);

						return;
					}

					Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterCreationSuccess, booster.GameID));
					RemoveBooster(booster.GameID, BoosterDequeueReason.Crafted);

					booster = GetNextCraftableBooster();
					if (booster == null) {
						// Queue has no more boosters in it
						return;
					}
				}

				BoosterInfosUpdateBackOffMultiplier = BoosterInfosUpdateBackOffMultiplierDefault;

				// Wait until the next booster is ready to craft
				DateTime nextBoosterTime = booster.GetAvailableAtTime();
				if (nextBoosterTime < DateTime.Now.AddSeconds(MinDelayBetweenBoosters)) {
					nextBoosterTime = DateTime.Now.AddSeconds(MinDelayBetweenBoosters);
				}

				UpdateTimer(nextBoosterTime);
				Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.NextBoosterCraft, String.Format("{0:T}", nextBoosterTime)));
			} finally {
				RunSemaphore.Release();
			}
		}

		internal void AddBooster(uint gameID, BoosterJob boosterJob) {
			void handler(Dictionary<uint, Steam.BoosterInfo> boosterInfos) {
				try {
					if (!boosterInfos.TryGetValue(gameID, out Steam.BoosterInfo? boosterInfo)) {
						// Bot cannot craft this booster
						Bot.ArchiLogger.LogGenericDebug(String.Format(Strings.BoosterUncraftable, gameID));
						boosterJob.OnBoosterUnqueueable(gameID, BoosterDequeueReason.Uncraftable);

						return;
					}

					if (!BoosterHandler.AllowCraftUnmarketableBoosters && !MarketableApps.AppIDs.Contains(gameID)) {
						// This booster is unmarketable and the plugin was configured to not craft marketable boosters
						Bot.ArchiLogger.LogGenericDebug(String.Format(Strings.BoosterUnmarketable, gameID));
						boosterJob.OnBoosterUnqueueable(gameID, BoosterDequeueReason.Unmarketable);

						return;
					}

					Booster booster = new Booster(Bot, gameID, boosterInfo, boosterJob);
					if (Boosters.Add(booster)) {
						Bot.ArchiLogger.LogGenericDebug(String.Format(Strings.BoosterQueued, gameID));
						boosterJob.OnBoosterQueued(booster);
					} else {
						boosterJob.OnBoosterUnqueueable(gameID, BoosterDequeueReason.AlreadyQueued);
					}
				} finally {
					OnBoosterInfosUpdated -= handler;
				}
			}

			OnBoosterInfosUpdated += handler;
		}

		internal bool RemoveBooster(uint gameID, BoosterDequeueReason reason, BoosterJob? boosterJob = null) {
			Booster? booster = Boosters.FirstOrDefault(booster => booster.GameID == gameID);
			if (booster == null) {
				return false;
			}

			if (boosterJob != null && booster.BoosterJob != boosterJob) {
				return false;
			}

			if (Boosters.Remove(booster)) {
				Bot.ArchiLogger.LogGenericDebug(String.Format(Strings.BoosterUnqueued, booster.GameID));
				booster.BoosterJob.OnBoosterDequeued(booster, reason);
				OnBoosterRemoved?.Invoke(booster, reason);

				return true;
			}

			return false;
		}

		internal Booster? GetNextCraftableBooster() {
			HashSet<Booster> uncraftedBoosters = Boosters.Where(booster => !booster.WasCrafted).ToHashSet<Booster>();
			if (uncraftedBoosters.Count == 0) {
				return null;
			}

			return uncraftedBoosters.OrderBy<Booster, DateTime>(booster => booster.GetAvailableAtTime()).First();
		}

		internal void ForceUpdateBoosterInfos(Dictionary<uint, Steam.BoosterInfo> _) => OnBoosterInfosUpdated -= ForceUpdateBoosterInfos;
		internal bool IsUpdatingBoosterInfos() => OnBoosterInfosUpdated != null;

		private async Task<Boolean> UpdateBoosterInfos() {
			if (OnBoosterInfosUpdated == null) {
				return true;
			}

			if (!BoosterHandler.AllowCraftUnmarketableBoosters && !await MarketableApps.Update().ConfigureAwait(false)) {
				return false;
			}

			(BoosterPageResponse? boosterPage, _) = await WebRequest.GetBoosterPage(Bot).ConfigureAwait(false);
			if (boosterPage == null) {
				Bot.ArchiLogger.LogNullError(boosterPage);

				return false;
			}

			GooAmount = boosterPage.GooAmount;
			TradableGooAmount = boosterPage.TradableGooAmount;
			UntradableGooAmount = boosterPage.UntradableGooAmount;
			OnBoosterInfosUpdated?.Invoke(boosterPage.BoosterInfos.ToDictionary(boosterInfo => boosterInfo.AppID));

			Bot.ArchiLogger.LogGenericDebug(Strings.BoosterInfoUpdateSuccess);

			return true;
		}

		private async Task<Boolean> CraftBooster(Booster booster) {
			Steam.TradabilityPreference nTp;
			if (!BoosterHandler.AllowCraftUntradableBoosters) {
				nTp = Steam.TradabilityPreference.Tradable;
			} else if (UntradableGooAmount > 0) {
				nTp = TradableGooAmount >= booster.Info?.Price ? Steam.TradabilityPreference.Tradable : Steam.TradabilityPreference.Untradable;
			} else {
				nTp = Steam.TradabilityPreference.Default;
			}
			
			Steam.BoostersResponse? result = await booster.Craft(nTp).ConfigureAwait(false);

			GooAmount = result?.GooAmount ?? GooAmount;
			TradableGooAmount = result?.TradableGooAmount ?? TradableGooAmount;
			UntradableGooAmount = result?.UntradableGooAmount ?? UntradableGooAmount;

			return booster.WasCrafted;
		}

		private void VerifyCraftBoosterError(Booster booster) {
			// Most errors we'll get when we try to create a booster will never go away. Retrying on an error will usually put us in an infinite loop.
			// Sometimes Steam will falsely report that an attempt to craft a booster failed, when it really didn't. It could also happen that the user crafted the booster on their own.
			// For any error we get, we'll need to refresh the booster page and see if the AvailableAtTime has changed to determine if we really failed to craft
			void handler(Dictionary<uint, Steam.BoosterInfo> boosterInfos) {
				try {
					Bot.ArchiLogger.LogGenericDebug(String.Format(Strings.BoosterCreationError, booster.GameID));

					if (!boosterInfos.TryGetValue(booster.GameID, out Steam.BoosterInfo? newBoosterInfo)) {
						// No longer have access to craft boosters for this game (game removed from account, or sometimes due to very rare Steam bugs)
						RemoveBooster(booster.GameID, BoosterDequeueReason.UnexpectedlyUncraftable);

						return;
					}

					if (newBoosterInfo.Unavailable && newBoosterInfo.AvailableAtTime != null
						&& newBoosterInfo.AvailableAtTime != booster.Info.AvailableAtTime
						&& (
							booster.Info.AvailableAtTime == null
							|| (newBoosterInfo.AvailableAtTime.Value - booster.Info.AvailableAtTime.Value).TotalHours > 2 // Make sure the change in time isn't due to daylight savings
						)
					) {
						Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterUnexpectedlyCrafted, booster.GameID));
						booster.SetWasCrafted();
						RemoveBooster(booster.GameID, BoosterDequeueReason.Crafted);

						return;
					}

					Bot.ArchiLogger.LogGenericDebug(String.Format(Strings.BoosterCreationRetry, booster.GameID));
				} finally {
					OnBoosterInfosUpdated -= handler;
				}
			}

			OnBoosterInfosUpdated += handler;
		}

		private void UpdateTimer(DateTime then) => Timer.Change(GetMillisecondsFromNow(then), Timeout.Infinite);
		private static int GetMillisecondsFromNow(DateTime then) => Math.Max(0, (int) (then - DateTime.Now).TotalMilliseconds);
	}
}