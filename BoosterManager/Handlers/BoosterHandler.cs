using ArchiSteamFarm.Steam;
using BoosterManager.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BoosterManager {
	internal sealed class BoosterHandler : IDisposable {
		private readonly Bot Bot;
		internal readonly BoosterDatabase? BoosterDatabase;
		internal readonly BoosterQueue BoosterQueue;
		internal static ConcurrentDictionary<string, BoosterHandler> BoosterHandlers = new();
		internal readonly List<BoosterJob> Jobs = new();
		private static int DelayBetweenBots = 0; // Delay, in minutes, between when bots will craft boosters
		internal static bool AllowCraftUntradableBoosters = true;
		internal static bool AllowCraftUnmarketableBoosters = true;

		private BoosterHandler(Bot bot) {
			Bot = bot;
			string databaseFilePath = Bot.GetFilePath(String.Format("{0}_{1}", bot.BotName, nameof(BoosterManager)), Bot.EFileType.Database);
			BoosterDatabase = BoosterDatabase.CreateOrLoad(databaseFilePath);
			BoosterQueue = new BoosterQueue(Bot);

			if (BoosterDatabase == null) {
				bot.ArchiLogger.LogGenericError(String.Format(ArchiSteamFarm.Localization.Strings.ErrorDatabaseInvalid, databaseFilePath));
			}
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

		internal string ScheduleBoosters(BoosterJobType jobType, HashSet<uint> gameIDs, StatusReporter craftingReporter) {
			Jobs.Add(new BoosterJob(Bot, jobType, gameIDs, craftingReporter));

			return Commands.FormatBotResponse(Bot, String.Format(Strings.BoosterCreationStarting, gameIDs.Count));
		}

		internal string UnscheduleBoosters(HashSet<uint>? gameIDs = null, int? timeLimitHours = null) {
			HashSet<uint> removedGameIDs = Jobs.RemoveBoosters(gameIDs, timeLimitHours);

			if (removedGameIDs.Count == 0) {
				if (timeLimitHours != null) {
					return Commands.FormatBotResponse(Bot, Strings.QueueRemovalByTimeFail);
				}

				return Commands.FormatBotResponse(Bot, Strings.QueueRemovalByAppFail);
			}

			return Commands.FormatBotResponse(Bot, String.Format(Strings.QueueRemovalSuccess, removedGameIDs.Count, String.Join(", ", removedGameIDs)));
		}

		internal void UpdateJobs() {
			Jobs.RemoveAll(job => job.IsFinished);
		}

		internal string GetStatus(bool shortStatus = false) {
			// Queue empty
			Booster? nextBooster = Jobs.NextBooster();
			Booster? limitedLastBooster = Jobs.Limited().LastBooster();
			if (nextBooster == null || limitedLastBooster == null) {
				if (BoosterQueue.IsUpdatingBoosterInfos()) {
					return Strings.BoosterInfoUpdating;
				}

				return Strings.QueueEmpty;
			}

			// Short status
			int limitedNumBoosters = Jobs.Limited().NumBoosters();
			int limitedGemsNeeded = Jobs.Limited().GemsNeeded();
			if (shortStatus) {
				return String.Format(Strings.QueueStatusShort, limitedNumBoosters, String.Format("{0:N0}", limitedGemsNeeded), String.Format("~{0:t}", limitedLastBooster.GetAvailableAtTime(BoosterQueue.BoosterDelay)));
			}

			// Long status
			List<string> responses = new List<string>();

			// Not enough gems
			int gemsNeeded = Jobs.GemsNeeded();
			if (gemsNeeded > BoosterQueue.AvailableGems) {
				responses.Add(String.Format("{0} :steamsad:", Strings.QueueStatusNotEnoughGems));

				if (nextBooster.Info.Price > BoosterQueue.AvailableGems) {
					responses.Add(String.Format(Strings.QueueStatusGemsNeeded, String.Format("{0:N0}", nextBooster.Info.Price - BoosterQueue.AvailableGems)));
				}

				if (Jobs.NumUncrafted() > 1) {
					responses.Add(String.Format(Strings.QueueStatusTotalGemsNeeded, String.Format("{0:N0}", gemsNeeded - BoosterQueue.AvailableGems)));
				}
			}

			// One-time booster status
			if (limitedNumBoosters > 0) {
				responses.Add(String.Format(Strings.QueueStatusOneTimeBoosters, Jobs.Limited().NumCrafted(), limitedNumBoosters, String.Format("~{0:t}", limitedLastBooster.GetAvailableAtTime(BoosterQueue.BoosterDelay)), String.Format("{0:N0}", limitedGemsNeeded)));
				responses.Add(String.Format(Strings.QueueStatusOneTimeBoosterList, String.Join(", ", Jobs.Limited().UncraftedGameIDs())));
			}

			// Permanent booster status
			if (Jobs.Permanent().NumBoosters() > 0) {
				responses.Add(String.Format(Strings.QueueStatusPermanentBoosters, String.Format("{0:N0}", Jobs.Permanent().GemsNeeded()), String.Join(", ", Jobs.Permanent().GameIDs())));
			}

			// Next booster to be crafted
			if (DateTime.Now > nextBooster.GetAvailableAtTime(BoosterQueue.BoosterDelay)) {
				responses.Add(String.Format(Strings.QueueStatusNextBoosterCraftingNow, nextBooster.Info.Name, nextBooster.GameID));
			} else {
				responses.Add(String.Format(Strings.QueueStatusNextBoosterCraftingLater, String.Format("{0:t}", nextBooster.GetAvailableAtTime(BoosterQueue.BoosterDelay)), nextBooster.Info.Name, nextBooster.GameID));
			}

			responses.Add("");

			return String.Join(Environment.NewLine, responses);
		}
		
		internal uint GetGemsNeeded() {
			int gemsNeeded = Jobs.GemsNeeded();
			if (BoosterQueue.AvailableGems > gemsNeeded) {
				return 0;
			}

			return (uint) (gemsNeeded - BoosterQueue.AvailableGems);
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
			return BoosterHandlers.Values.Any(handler => handler.Jobs.Limited().GemsNeeded() > 0);
		}
	}
}
