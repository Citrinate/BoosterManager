using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using Newtonsoft.Json;

namespace BoosterManager {
	internal sealed class BoosterQueue : IDisposable {
		private readonly Bot Bot;
		private readonly BoosterHandler BoosterHandler;
		private readonly Timer Timer;
		private readonly ConcurrentDictionary<uint, Booster> Boosters = new();
		private Dictionary<uint, Steam.BoosterInfo> BoosterInfos = new();
		private uint GooAmount = 0;
		private uint TradableGooAmount = 0;
		private uint UntradableGooAmount = 0;
		private const int MinDelayBetweenBoosters = 5; // Minimum delay, in seconds, between booster crafts
		internal int BoosterDelay = 0; // Delay, in minutes, added to all booster crafts
		internal event Action? OnBoosterInfosUpdated;

		internal BoosterQueue(Bot bot, BoosterHandler boosterHandler) {
			Bot = bot;
			BoosterHandler = boosterHandler;
			Timer = new Timer(
				async e => await Run().ConfigureAwait(false),
				null, 
				Timeout.Infinite, 
				Timeout.Infinite
			);
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
				Bot.ArchiLogger.LogGenericInfo("Failed to update booster information");
				UpdateTimer(DateTime.Now.AddMinutes(1));

				return;
			}

			Booster? booster = GetNextCraftableBooster(BoosterType.Any);
			if (booster == null) {
				return;
			}
			if (DateTime.Now >= booster.GetAvailableAtTime().AddMinutes(BoosterDelay)) {
				if (booster.Info.Price > GooAmount) {
					BoosterHandler.PerpareStatusReport(String.Format("{0:N0} more gems are needed to finish crafting boosters. Crafting will resume when more gems are available.", GetGemsNeeded(BoosterType.Any, wasCrafted: false) - GooAmount), suppressDuplicateMessages: true);
					OnBoosterInfosUpdated += ForceUpdateBoosterInfos;
					UpdateTimer(DateTime.Now.AddMinutes(GetNumBoosters(BoosterType.OneTime) > 0 ? 1 : 15));

					return;
				}

				if (!await CraftBooster(booster).ConfigureAwait(false)) {
					Bot.ArchiLogger.LogGenericInfo(String.Format("Failed to create booster from {0}", booster.GameID));
					VerifyCraftBoosterError(booster);
					UpdateTimer(DateTime.Now);

					return;
				}

				Bot.ArchiLogger.LogGenericInfo(String.Format("Successfuly created booster from {0}", booster.GameID));
				if (IsFinished(booster.Type)) {
					if (booster.Type == BoosterType.OneTime) {
						BoosterHandler.PerpareStatusReport(String.Format("Finished crafting {0} boosters!", GetNumBoosters(BoosterType.OneTime)));
					}
					ClearCraftedBoosters(booster.Type);

					return;
				}

				booster = GetNextCraftableBooster(BoosterType.Any);
				if (booster == null) {
					return;
				}
			}

			DateTime nextBoosterTime = booster.GetAvailableAtTime().AddMinutes(BoosterDelay);
			if (nextBoosterTime < DateTime.Now.AddSeconds(MinDelayBetweenBoosters)) {
				nextBoosterTime = DateTime.Now.AddSeconds(MinDelayBetweenBoosters);
			}
			UpdateTimer(nextBoosterTime);
			Bot.ArchiLogger.LogGenericInfo(String.Format("Next booster will be crafted at {0:h:mm:ss tt}", nextBoosterTime));
		}

		internal void AddBooster(uint gameID, BoosterType type) {
			void handler() {
				if (BoosterInfos.TryGetValue(gameID, out Steam.BoosterInfo? boosterInfo)) {
					Booster booster = new Booster(Bot, gameID, type, boosterInfo);
					if (Boosters.TryAdd(gameID, booster)) {
						Bot.ArchiLogger.LogGenericInfo(String.Format("Added {0} to booster queue.", gameID));
					}
				} else {
					Bot.ArchiLogger.LogGenericInfo(String.Format("Can't craft boosters for {0}", gameID));
				}
				OnBoosterInfosUpdated -= handler;
			}
			OnBoosterInfosUpdated += handler;
		}

		private bool RemoveBooster(uint gameID) {
			if (Boosters.TryRemove(gameID, out Booster? booster)) {
				if (booster.Type == BoosterType.Permanent) {
					Bot.ArchiLogger.LogGenericInfo(String.Format("Re-adding permanent {0} to booster queue.", gameID));
					AddBooster(gameID, BoosterType.Permanent);

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

			IDocument? boosterPage = await WebRequest.GetBoosterPage(Bot).ConfigureAwait(false);
			if (boosterPage == null) {
				Bot.ArchiLogger.LogNullError(boosterPage);

				return false;
			}

			MatchCollection gooAmounts = Regex.Matches(boosterPage.Source.Text, "(?<=parseFloat\\( \")[0-9]+");
			Match info = Regex.Match(boosterPage.Source.Text, "\\[\\{\"[\\s\\S]*\"}]");
			if (!info.Success || (gooAmounts.Count != 3)) {
				Bot.ArchiLogger.LogGenericError(string.Format(Strings.ErrorParsingObject, boosterPage));

				return false;
			}
			GooAmount = uint.Parse(gooAmounts[0].Value);
			TradableGooAmount = uint.Parse(gooAmounts[1].Value);
			UntradableGooAmount = uint.Parse(gooAmounts[2].Value);

			IEnumerable<Steam.BoosterInfo>? enumerableBoosters;
			try {
				enumerableBoosters = JsonConvert.DeserializeObject<IEnumerable<Steam.BoosterInfo>>(info.Value, new Steam.BoosterInfoDateConverter());
			} catch (JsonException ex) {
				Bot.ArchiLogger.LogGenericError(ex.Message);

				return false;
			}
			if (enumerableBoosters == null) {
				Bot.ArchiLogger.LogNullError(enumerableBoosters);

				return false;
			}
			BoosterInfos = enumerableBoosters.ToDictionary(boosterInfo => boosterInfo.AppID);
			Bot.ArchiLogger.LogGenericInfo("BoosterInfos updated");
			OnBoosterInfosUpdated?.Invoke();

			return true;
		}

		private async Task<Boolean> CraftBooster(Booster booster) {
			TradabilityPreference nTp;
			if (UntradableGooAmount > 0) {
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
				if (BoosterInfos.TryGetValue(booster.GameID, out Steam.BoosterInfo? boosterInfo)) {
					if (boosterInfo.Unavailable && boosterInfo.AvailableAtTime != booster.Info.AvailableAtTime) {
						Bot.ArchiLogger.LogGenericInfo(String.Format("Booster from {0} was recently created either by us or by user", booster.GameID));
						booster.WasCrafted = true;
					}
				} else {
					// No longer have access to craft boosters for this game (game removed from account, or sometimes due to very rare Steam bugs)
					BoosterHandler.PerpareStatusReport(String.Format("No longer able to craft boosters from {0} ({1})", booster.Info.Name, booster.GameID));
					RemoveBooster(booster.GameID);
				}
				OnBoosterInfosUpdated -= handler;
			}
			OnBoosterInfosUpdated += handler;
		}

		private Booster? GetNextCraftableBooster(BoosterType type, bool getLast = false) {
			HashSet<Booster> uncraftedBoosters = GetBoosters(type, wasCrafted: false);
			if (uncraftedBoosters.Count == 0) {
				return null;
			}

			if (getLast) {
				return uncraftedBoosters.MaxBy(booster => booster.GetAvailableAtTime());
			}

			return uncraftedBoosters.MinBy(booster => booster.GetAvailableAtTime());
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
						Bot.ArchiLogger.LogGenericInfo(String.Format("User removed {0} from booster queue.", gameID));
						removedGameIDs.Add(gameID);
					}
				}
			}
			if (IsFinished(BoosterType.OneTime)) {
				BoosterHandler.PerpareStatusReport(String.Format("Finished crafting {0} boosters!", GetNumBoosters(BoosterType.OneTime)));
				ClearCraftedBoosters(BoosterType.OneTime);
			}

			return removedGameIDs;
		}

		internal string? GetShortStatus() {
			Booster? lastOneTimeBooster = GetNextCraftableBooster(BoosterType.OneTime, getLast: true);
			if (lastOneTimeBooster == null) {
				return null;
			}

			return String.Format("{0} booster(s) from {1:N0} gems will be crafted by ~{2:h:mm tt}", GetNumBoosters(BoosterType.OneTime), GetGemsNeeded(BoosterType.OneTime), lastOneTimeBooster.GetAvailableAtTime().AddMinutes(BoosterDelay));
		}

		internal string GetStatus() {
			Booster? nextBooster = GetNextCraftableBooster(BoosterType.Any);
			if (nextBooster == null) {
				if (OnBoosterInfosUpdated != null) {
					return "Updating booster information...";
				}

				return "Bot is not crafting any boosters.";
			}

			HashSet<string> responses = new HashSet<string>();
			if (GetGemsNeeded(BoosterType.Any, wasCrafted: false) > GooAmount) {
				responses.Add("Not enough gems!");
				if (nextBooster.Info.Price > GooAmount) {
					responses.Add(String.Format("Need {0:N0} more gems for the next booster!", nextBooster.Info.Price - GooAmount));
				}
				if (GetNumBoosters(BoosterType.Any, wasCrafted: false) > 1) {
					responses.Add(String.Format("Need {0:N0} more gems to finish all boosters!", GetGemsNeeded(BoosterType.Any, wasCrafted: false) - GooAmount));
				}
			}
			if (GetNumBoosters(BoosterType.OneTime) > 0) {
				Booster? lastOneTimeBooster = GetNextCraftableBooster(BoosterType.OneTime, getLast: true);
				if (lastOneTimeBooster != null) {
					responses.Add(String.Format("Crafted {0}/{1} one-time boosters. Crafting will finish at ~{2:h:mm tt}, and will use {3:N0} gems.", GetNumBoosters(BoosterType.OneTime, wasCrafted: true), GetNumBoosters(BoosterType.OneTime), lastOneTimeBooster.GetAvailableAtTime().AddMinutes(BoosterDelay), GetGemsNeeded(BoosterType.OneTime, wasCrafted: false)));
					responses.Add(String.Format("One-time boosters waiting to be crafted: {0}", String.Join(", ", GetBoosterIDs(BoosterType.OneTime, wasCrafted: false))));
				}
			}
			if (GetNumBoosters(BoosterType.Permanent) > 0) {
				responses.Add(String.Format("Permanent boosters that will be crafted continually for {0:N0} gems: {1}", GetGemsNeeded(BoosterType.Permanent), String.Join(", ", GetBoosterIDs(BoosterType.Permanent))));
			}
			responses.Add(String.Format("Next booster will be crafted at {0:h:mm tt}: {1} ({2})", nextBooster.GetAvailableAtTime().AddMinutes(BoosterDelay), nextBooster.Info.Name, nextBooster.GameID));
			responses.Add("");

			return String.Join(Environment.NewLine, responses);
		}

		private bool FilterBoosterByType(Booster booster, BoosterType type) => type == BoosterType.Any || booster.Type == type;
		private HashSet<Booster> GetBoosters(BoosterType type, bool? wasCrafted = null) => Boosters.Values.Where(booster => (wasCrafted == null || booster.WasCrafted == wasCrafted) && FilterBoosterByType(booster, type)).ToHashSet<Booster>();
		private HashSet<uint> GetBoosterIDs(BoosterType type, bool? wasCrafted = null) => GetBoosters(type, wasCrafted).Select(booster => booster.GameID).ToHashSet<uint>();
		private int GetNumBoosters(BoosterType type, bool? wasCrafted = null) => GetBoosters(type, wasCrafted).Count;
		private int GetGemsNeeded(BoosterType type, bool? wasCrafted = null) => GetBoosters(type, wasCrafted).Sum(booster => (int) booster.Info.Price);
		private bool IsFinished(BoosterType type) => GetNumBoosters(type, wasCrafted: true) > 0 && GetNumBoosters(type, wasCrafted: false) == 0;
		private void ForceUpdateBoosterInfos() => OnBoosterInfosUpdated -= ForceUpdateBoosterInfos;
		private static int GetMillisecondsFromNow(DateTime then) => Math.Max(0, (int) (then - DateTime.Now).TotalMilliseconds);
		private void UpdateTimer(DateTime then) => Timer.Change(GetMillisecondsFromNow(then), Timeout.Infinite);
	}
}