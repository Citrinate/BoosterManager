using ArchiSteamFarm.Steam;
using BoosterManager.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BoosterManager {
	internal sealed class BoosterHandler : IDisposable {
		private readonly Bot Bot;
		private readonly BoosterQueue BoosterQueue;
		internal static ConcurrentDictionary<string, BoosterHandler> BoosterHandlers = new();
		internal static StatusReporter GeneralReporter = new(); // Used to report when an unexpected booster error has occured, or when booster crafting is finished
		private static int DelayBetweenBots = 0; // Delay, in minutes, between when bots will craft boosters
		internal static bool AllowCraftUntradableBoosters = true;
		internal static bool AllowCraftUnmarketableBoosters = true;

		private BoosterHandler(Bot bot) {
			Bot = bot;
			BoosterQueue = new BoosterQueue(Bot);
			GeneralReporter.Update(bot, bot.Actions.GetFirstSteamMasterID());
		}

		public void Dispose() {
			BoosterQueue.Dispose();
		}

		internal static void AddHandler(Bot bot) {
			if (BoosterHandlers.ContainsKey(bot.BotName)) {
				BoosterHandlers[bot.BotName].Dispose();
				BoosterHandlers.TryRemove(bot.BotName, out BoosterHandler? _);
			}

			if (BoosterHandlers.TryAdd(bot.BotName, new BoosterHandler(bot))) {
				UpdateBotDelays();
			}
		}

		internal static void UpdateBotDelays(int? delayInSeconds = null) {
			if (DelayBetweenBots == 0 && (delayInSeconds == null || delayInSeconds == 0)) {
				return;
			}

			// This feature only exists because it existed in Outzzz's BoosterCreator plugin.  I don't think it's all that useful.
			
			// This assumes that the same bots will be used all of the time, with the same names, and all boosters will be 
			// crafted when they're scheduled to be crafted (no unexpected delays due to Steam downtime or insufficient gems).  
			// If all of these things are true then BoosterDelayBetweenBots should work as it's described in the README.  If these 
			// assumptions are not met, then the delay between bots might become lower than intended, but it should never be higher
			// I don't intend to fix this.
			// A workaround for users caught in an undesirable state is to let the 24-hour cooldown on all of their boosters expire.

			DelayBetweenBots = delayInSeconds ?? DelayBetweenBots;
			List<string> botNames = BoosterHandlers.Keys.ToList<string>();
			botNames.Sort();

			foreach (KeyValuePair<string, BoosterHandler> kvp in BoosterHandlers) {
				int index = botNames.IndexOf(kvp.Key);
				kvp.Value.BoosterQueue.BoosterDelay = DelayBetweenBots * index;
			}
		}

		internal string ScheduleBoosters(HashSet<uint> gameIDs, StatusReporter craftingReporter) {
			foreach (uint gameID in gameIDs) {
				BoosterQueue.AddBooster(gameID, BoosterType.OneTime);
			}

			void handler() {
				try {
					string? message = BoosterQueue.GetShortStatus();
					if (message == null) {
						craftingReporter.Report(Bot, Strings.BoostersUncraftable);

						return;
					}

					craftingReporter.Report(Bot, message);
				} finally {
					BoosterQueue.OnBoosterInfosUpdated -= handler;
				}
			}

			GeneralReporter.Update(craftingReporter);
			BoosterQueue.OnBoosterInfosUpdated += handler;
			BoosterQueue.Start();

			return Commands.FormatBotResponse(Bot, String.Format(Strings.BoosterCreationStarting, gameIDs.Count));
		}

		internal void SchedulePermanentBoosters(HashSet<uint> gameIDs) {
			foreach (uint gameID in gameIDs) {
				BoosterQueue.AddBooster(gameID, BoosterType.Permanent);
			}

			BoosterQueue.Start();
		}

		internal string UnscheduleBoosters(HashSet<uint>? gameIDs = null, int? timeLimitHours = null) {
			HashSet<uint> removedGameIDs = BoosterQueue.RemoveBoosters(gameIDs, timeLimitHours);

			if (removedGameIDs.Count == 0) {
				if (timeLimitHours == null) {
					return Commands.FormatBotResponse(Bot, Strings.QueueRemovalByAppFail);
				}
				
				return Commands.FormatBotResponse(Bot, Strings.QueueRemovalByTimeFail);

			}

			return Commands.FormatBotResponse(Bot, String.Format(Strings.QueueRemovalSuccess, removedGameIDs.Count, String.Join(", ", removedGameIDs)));
		}

		internal string GetStatus(bool shortStatus = false) {
			if (shortStatus) {
				return Commands.FormatBotResponse(Bot, BoosterQueue.GetShortStatus() ?? BoosterQueue.GetStatus());
			}

			return Commands.FormatBotResponse(Bot, BoosterQueue.GetStatus());
		}
		
		internal uint GetGemsNeeded() {
			if (BoosterQueue.GetAvailableGems() > BoosterQueue.GetGemsNeeded(BoosterType.Any, wasCrafted: false)) {
				return 0;
			}

			return (uint) (BoosterQueue.GetGemsNeeded(BoosterType.Any, wasCrafted: false) - BoosterQueue.GetAvailableGems());
		}

		internal void OnGemsRecieved() {
			if (GetGemsNeeded() == 0) {
				return;
			}

			// Refresh gems count
			BoosterQueue.OnBoosterInfosUpdated += BoosterQueue.ForceUpdateBoosterInfos;
			BoosterQueue.Start();
		}

		internal static bool IsCraftingOneTimeBoosters() {
			return BoosterHandlers.Any(handler => handler.Value.BoosterQueue.GetNumBoosters(BoosterType.OneTime, wasCrafted: false) > 0);
		}

		private static int GetMillisecondsFromNow(DateTime then) => Math.Max(0, (int) (then - DateTime.Now).TotalMilliseconds);
	}
}
