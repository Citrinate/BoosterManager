using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;

namespace BoosterManager {
	internal sealed class BoosterQueue : IDisposable {
		private readonly Bot Bot;
		private readonly Timer Timer;
		private readonly ConcurrentDictionary<uint, Booster> Boosters = new();
		private Dictionary<uint, Steam.BoosterInfo> BoosterInfos = new();
		private uint GooAmount = 0;
		private uint TradableGooAmount = 0;
		private uint UntradableGooAmount = 0;
		private const int MinDelayBetweenBoosters = 5; // Minimum delay, in seconds, between booster crafts
		internal int BoosterDelay = 0; // Delay, in seconds, added to all booster crafts
		private readonly BoosterDatabase? BoosterDatabase;
		internal event Action? OnBoosterInfosUpdated;
		internal event Action? OnBoosterFinishedCheck;
		private float BoosterInfosUpdateBackOffMultiplier = 1.0F;

		internal BoosterQueue(Bot bot) {
			Bot = bot;
			Timer = new Timer(
				async e => await Run().ConfigureAwait(false),
				null, 
				Timeout.Infinite, 
				Timeout.Infinite
			);

			string databaseFilePath = Bot.GetFilePath(String.Format("{0}_{1}", bot.BotName, nameof(BoosterManager)), Bot.EFileType.Database);
			BoosterDatabase = BoosterDatabase.CreateOrLoad(databaseFilePath);

			if (BoosterDatabase == null) {
				bot.ArchiLogger.LogGenericError(String.Format(ArchiSteamFarm.Localization.Strings.ErrorDatabaseInvalid, databaseFilePath));
			}
		}

		public void Dispose() {
			Timer.Dispose();
		}

		internal void Start() {
			UpdateTimer(DateTime.Now);
		}

		private async Task Run() {
			if (!Bot.IsConnectedAndLoggedOn) {
				UpdateTimer(DateTime.Now.AddSeconds(1));

				return;
			}

			if (!await UpdateBoosterInfos().ConfigureAwait(false)) {
				Bot.ArchiLogger.LogGenericError(Strings.BoosterInfoUpdateFailed);
				UpdateTimer(DateTime.Now.AddMinutes(Math.Min(15, 1 * BoosterInfosUpdateBackOffMultiplier)));
				BoosterInfosUpdateBackOffMultiplier += 0.5F;

				return;
			}

			Booster? booster = GetNextCraftableBooster(BoosterType.Any);
			if (booster == null) {
				BoosterInfosUpdateBackOffMultiplier = 1.0F;

				return;
			}
			
			if (DateTime.Now >= booster.GetAvailableAtTime(BoosterDelay)) {
				if (booster.Info.Price > GetAvailableGems()) {
					BoosterHandler.GeneralReporter.Report(Bot, String.Format(Strings.NotEnoughGems, String.Format("{0:N0}", GetGemsNeeded(BoosterType.Any, wasCrafted: false) - GetAvailableGems())), suppressDuplicateMessages: true);
					OnBoosterInfosUpdated += ForceUpdateBoosterInfos;
					UpdateTimer(DateTime.Now.AddMinutes(Math.Min(15, (GetNumBoosters(BoosterType.OneTime) > 0 ? 1 : 15) * BoosterInfosUpdateBackOffMultiplier)));
					BoosterInfosUpdateBackOffMultiplier += 0.5F;

					return;
				}

				BoosterInfosUpdateBackOffMultiplier = 1.0F;

				if (!await CraftBooster(booster).ConfigureAwait(false)) {
					Bot.ArchiLogger.LogGenericError(String.Format(Strings.BoosterCreationFailed, booster.GameID));
					VerifyCraftBoosterError(booster);
					UpdateTimer(DateTime.Now);

					return;
				}

				Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterCreationSuccess, booster.GameID));
				CheckIfFinished(booster.Type);

				booster = GetNextCraftableBooster(BoosterType.Any);
				if (booster == null) {
					return;
				}
			}

			BoosterInfosUpdateBackOffMultiplier = 1.0F;

			DateTime nextBoosterTime = booster.GetAvailableAtTime(BoosterDelay);
			if (nextBoosterTime < DateTime.Now.AddSeconds(MinDelayBetweenBoosters)) {
				nextBoosterTime = DateTime.Now.AddSeconds(MinDelayBetweenBoosters);
			}
			UpdateTimer(nextBoosterTime);
			Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.NextBoosterCraft, String.Format("{0:T}", nextBoosterTime)));
		}

		internal void AddBooster(uint gameID, BoosterType type) {
			void handler() {
				try {
					if (!BoosterInfos.TryGetValue(gameID, out Steam.BoosterInfo? boosterInfo)) {
						Bot.ArchiLogger.LogGenericError(String.Format(Strings.BoosterUncraftable, gameID));

						return;
					}

					if (Boosters.TryGetValue(gameID, out Booster? existingBooster)) {
						// Re-add a booster that was successfully crafted and is waiting to be cleared out of the queue
						if (existingBooster.Type == BoosterType.OneTime && existingBooster.WasCrafted) {
							RemoveBooster(gameID);
						}
					}

					if (!BoosterHandler.AllowCraftUnmarketableBoosters && !MarketableApps.AppIDs.Contains(gameID)) {
						Bot.ArchiLogger.LogGenericError(String.Format(Strings.BoosterUnmarketable, gameID));

						return;
					}

					Booster newBooster = new Booster(Bot, gameID, type, boosterInfo, this, GetLastCraft(gameID));
					if (Boosters.TryAdd(gameID, newBooster)) {
						Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterQueued, gameID));
					}
				} finally {
					OnBoosterInfosUpdated -= handler;
				}
			}

			OnBoosterInfosUpdated += handler;
		}

		private bool RemoveBooster(uint gameID) {
			if (Boosters.TryRemove(gameID, out Booster? booster)) {
				Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterUnqueued, gameID));
				if (booster.Type == BoosterType.Permanent) {
					Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.PermanentBoosterRequeued, gameID));
					AddBooster(gameID, BoosterType.Permanent);
					UpdateTimer(DateTime.Now.AddSeconds(MinDelayBetweenBoosters));

					return false;
				}

				return true;
			}

			return false;
		}

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
			BoosterInfos = boosterPage.BoosterInfos.ToDictionary(boosterInfo => boosterInfo.AppID);

			Bot.ArchiLogger.LogGenericInfo(Strings.BoosterInfoUpdateSuccess);
			OnBoosterInfosUpdated?.Invoke();

			return true;
		}

		private async Task<Boolean> CraftBooster(Booster booster) {
			TradabilityPreference nTp;
			if (!BoosterHandler.AllowCraftUntradableBoosters) {
				nTp = TradabilityPreference.Tradable;
			} else if (UntradableGooAmount > 0) {
				nTp = TradableGooAmount >= booster.Info?.Price ? TradabilityPreference.Tradable : TradabilityPreference.Untradable;
			} else {
				nTp = TradabilityPreference.Default;
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
			void handler() {
				try {
					Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterCreationError, booster.GameID));

					if (!BoosterInfos.TryGetValue(booster.GameID, out Steam.BoosterInfo? newBoosterInfo)) {
						// No longer have access to craft boosters for this game (game removed from account, or sometimes due to very rare Steam bugs)
						BoosterHandler.GeneralReporter.Report(Bot, String.Format(Strings.BoosterUnexpectedlyUncraftable, booster.Info.Name, booster.GameID));
						RemoveBooster(booster.GameID);
						CheckIfFinished(booster.Type);

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
						CheckIfFinished(booster.Type);

						return;
					}

					Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterCreationRetry, booster.GameID));
				} finally {
					OnBoosterInfosUpdated -= handler;
				}
			}

			OnBoosterInfosUpdated += handler;
		}

		private Booster? GetNextCraftableBooster(BoosterType type, bool getLast = false, HashSet<uint>? filterGameIDs = null) {
			HashSet<Booster> uncraftedBoosters = GetBoosters(type, wasCrafted: false, filterGameIDs: filterGameIDs);
			if (uncraftedBoosters.Count == 0) {
				return null;
			}

			IOrderedEnumerable<Booster> orderedUncraftedBoosters = uncraftedBoosters.OrderBy<Booster, DateTime>(booster => booster.GetAvailableAtTime());
			if (getLast) {
				return orderedUncraftedBoosters.Last();
			}

			return orderedUncraftedBoosters.First();
		}

		internal bool CheckIfFinished(BoosterType type, HashSet<uint>? filterGameIDs = null) {
			OnBoosterFinishedCheck?.Invoke();

			if (!IsFinishedCrafting(type)) {
				return false;
			}

			ClearCraftedBoosters(type);

			return true;
		}

		private void ClearCraftedBoosters(BoosterType type) {
			HashSet<Booster> boosters = GetBoosters(type, wasCrafted: true);
			foreach (Booster booster in boosters) {
				RemoveBooster(booster.GameID);
			}
		}

		internal HashSet<uint> RemoveBoosters(HashSet<uint>? gameIDs = null, int? timeLimitHours = null) {
			if (gameIDs == null) {
				gameIDs = new HashSet<uint>();
			}
			if (timeLimitHours != null) {
				if (timeLimitHours == 0) {
					// Cancel everything
					gameIDs.UnionWith(GetBoosterIDs(BoosterType.OneTime));
				} else {
					DateTime timeLimit = DateTime.Now.AddHours(timeLimitHours.Value);
					HashSet<uint> timeFilteredGameIDs = GetBoosters(BoosterType.OneTime).Where(booster => booster.GetAvailableAtTime() >= timeLimit).Select(booster => booster.GameID).ToHashSet<uint>();
					gameIDs.UnionWith(timeFilteredGameIDs);
				}
			}
			HashSet<uint> removedGameIDs = new HashSet<uint>();
			foreach (uint gameID in gameIDs) {
				if (Boosters.TryGetValue(gameID, out Booster? booster)) {
					if (booster.WasCrafted) {
						continue;
					}

					if (RemoveBooster(gameID)) {
						Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterUnqueuedByUser, gameID));
						removedGameIDs.Add(gameID);
					}
				}
			}
			CheckIfFinished(BoosterType.OneTime);
			CheckIfFinished(BoosterType.Permanent);

			return removedGameIDs;
		}

		internal string? GetShortStatus(HashSet<uint>? filterGameIDs = null) {
			Booster? lastOneTimeBooster = GetNextCraftableBooster(BoosterType.OneTime, getLast: true, filterGameIDs: filterGameIDs);
			if (lastOneTimeBooster == null) {
				return null;
			}

			return String.Format(Strings.QueueStatusShort, GetNumBoosters(BoosterType.OneTime, filterGameIDs: filterGameIDs), String.Format("{0:N0}", GetGemsNeeded(BoosterType.OneTime, filterGameIDs: filterGameIDs)), String.Format("~{0:t}", lastOneTimeBooster.GetAvailableAtTime(BoosterDelay)));
		}

		internal string GetStatus() {
			Booster? nextBooster = GetNextCraftableBooster(BoosterType.Any);
			if (nextBooster == null) {
				if (OnBoosterInfosUpdated != null) {
					return Strings.BoosterInfoUpdating;
				}

				return Strings.QueueEmpty;
			}

			HashSet<string> responses = new HashSet<string>();

			// Not enough gems
			if (GetGemsNeeded(BoosterType.Any, wasCrafted: false) > GetAvailableGems()) {
				responses.Add(String.Format("{0} :steamsad:", Strings.QueueStatusNotEnoughGems));

				if (nextBooster.Info.Price > GetAvailableGems()) {
					responses.Add(String.Format(Strings.QueueStatusGemsNeeded, String.Format("{0:N0}", nextBooster.Info.Price - GetAvailableGems())));
				}

				if (GetNumBoosters(BoosterType.Any, wasCrafted: false) > 1) {
					responses.Add(String.Format(Strings.QueueStatusTotalGemsNeeded, String.Format("{0:N0}", GetGemsNeeded(BoosterType.Any, wasCrafted: false) - GetAvailableGems())));
				}
			}

			// One time booster status
			if (GetNumBoosters(BoosterType.OneTime) > 0) {
				Booster? lastOneTimeBooster = GetNextCraftableBooster(BoosterType.OneTime, getLast: true);
				if (lastOneTimeBooster != null) {
					responses.Add(String.Format(Strings.QueueStatusOneTimeBoosters, GetNumBoosters(BoosterType.OneTime, wasCrafted: true), GetNumBoosters(BoosterType.OneTime), String.Format("~{0:t}", lastOneTimeBooster.GetAvailableAtTime(BoosterDelay)), String.Format("{0:N0}", GetGemsNeeded(BoosterType.OneTime, wasCrafted: false))));
					responses.Add(String.Format(Strings.QueueStatusOneTimeBoosterList, String.Join(", ", GetBoosterIDs(BoosterType.OneTime, wasCrafted: false))));
				}
			}

			// Permanent booster status
			if (GetNumBoosters(BoosterType.Permanent) > 0) {
				responses.Add(String.Format(Strings.QueueStatusPermanentBoosters, String.Format("{0:N0}", GetGemsNeeded(BoosterType.Permanent)), String.Join(", ", GetBoosterIDs(BoosterType.Permanent))));
			}

			// Next booster to be crafted
			if (DateTime.Now > nextBooster.GetAvailableAtTime(BoosterDelay)) {
				responses.Add(String.Format(Strings.QueueStatusNextBoosterCraftingNow, nextBooster.Info.Name, nextBooster.GameID));
			} else {
				responses.Add(String.Format(Strings.QueueStatusNextBoosterCraftingLater, String.Format("{0:t}", nextBooster.GetAvailableAtTime(BoosterDelay)), nextBooster.Info.Name, nextBooster.GameID));
			}

			responses.Add("");

			return String.Join(Environment.NewLine, responses);
		}

		private bool FilterBoosterByType(Booster booster, BoosterType type) => type == BoosterType.Any || booster.Type == type;
		private HashSet<Booster> GetBoosters(BoosterType type, bool? wasCrafted = null, HashSet<uint>? filterGameIDs = null) => Boosters.Values.Where(booster => (filterGameIDs == null || filterGameIDs.Contains(booster.GameID)) && (wasCrafted == null || booster.WasCrafted == wasCrafted) && FilterBoosterByType(booster, type)).ToHashSet<Booster>();
		private HashSet<uint> GetBoosterIDs(BoosterType type, bool? wasCrafted = null, HashSet<uint>? filterGameIDs = null) => GetBoosters(type, wasCrafted, filterGameIDs).Select(booster => booster.GameID).ToHashSet<uint>();
		internal int GetNumBoosters(BoosterType type, bool? wasCrafted = null, HashSet<uint>? filterGameIDs = null) => GetBoosters(type, wasCrafted, filterGameIDs).Count;
		internal int GetGemsNeeded(BoosterType type, bool? wasCrafted = null, HashSet<uint>? filterGameIDs = null) => GetBoosters(type, wasCrafted, filterGameIDs).Sum(booster => (int) booster.Info.Price);
		internal bool IsFinishedCrafting(BoosterType type, HashSet<uint>? filterGameIDs = null) => GetNumBoosters(type, wasCrafted: true, filterGameIDs) > 0 && GetNumBoosters(type, wasCrafted: false, filterGameIDs) == 0;
		internal void ForceUpdateBoosterInfos() => OnBoosterInfosUpdated -= ForceUpdateBoosterInfos;
		private static int GetMillisecondsFromNow(DateTime then) => Math.Max(0, (int) (then - DateTime.Now).TotalMilliseconds);
		private void UpdateTimer(DateTime then) => Timer.Change(GetMillisecondsFromNow(then), Timeout.Infinite);
		internal uint GetAvailableGems() => BoosterHandler.AllowCraftUntradableBoosters ? GooAmount : TradableGooAmount;
		internal BoosterLastCraft? GetLastCraft(uint appID) => BoosterDatabase?.GetLastCraft(appID);
		internal void UpdateLastCraft(uint appID, DateTime craftTime) => BoosterDatabase?.SetLastCraft(appID, craftTime, BoosterDelay);
	}
}