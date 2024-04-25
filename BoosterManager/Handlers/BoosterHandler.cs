using ArchiSteamFarm.Collections;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BoosterManager {
	internal sealed class BoosterHandler : IDisposable {
		private readonly Bot Bot;
		internal readonly BoosterDatabase BoosterDatabase;
		internal readonly BoosterQueue BoosterQueue;
		internal static ConcurrentDictionary<string, BoosterHandler> BoosterHandlers = new();
		private ConcurrentList<BoosterJob> Jobs = new();
		internal static bool AllowCraftUntradableBoosters = true;
		internal static bool AllowCraftUnmarketableBoosters = true;

		private BoosterHandler(Bot bot, BoosterDatabase boosterDatabase) {
			ArgumentNullException.ThrowIfNull(boosterDatabase);

			Bot = bot;
			BoosterDatabase = boosterDatabase;
			BoosterQueue = new BoosterQueue(Bot);
		}

		public void Dispose() {
			BoosterQueue.Dispose();
		}

		internal static void AddHandler(Bot bot, BoosterDatabase boosterDatabase) {
			if (BoosterHandlers.ContainsKey(bot.BotName)) {
				BoosterHandlers[bot.BotName].Dispose();
				BoosterHandlers.TryRemove(bot.BotName, out BoosterHandler? _);
			}

			BoosterHandler handler = new BoosterHandler(bot, boosterDatabase);
			if (BoosterHandlers.TryAdd(bot.BotName, handler)) {
				handler.RestoreBoosterJobs();
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

		internal void UpdateBoosterJobs() {
			Jobs.Finished().ToList().ForEach(job => Jobs.Remove(job));
			BoosterDatabase.UpdateBoosterJobs(Jobs.Limited().Unfinised().SaveState());
		}

		private void RestoreBoosterJobs() {
			foreach (BoosterJobState jobState in BoosterDatabase.BoosterJobs) {
				Jobs.Add(new BoosterJob(Bot, BoosterJobType.Limited, jobState));
			}
		}

		internal string GetStatus(bool shortStatus = false) {
			// Queue empty
			Booster? nextBooster = Jobs.NextBooster();
			Booster? limitedLastBooster = Jobs.Limited().LastBooster();
			if (nextBooster == null || limitedLastBooster == null) {
				if (BoosterQueue.IsUpdatingBoosterInfos()) {
					return Commands.FormatBotResponse(Bot, Strings.BoosterInfoUpdating);
				}

				return Commands.FormatBotResponse(Bot, Strings.QueueEmpty);
			}

			// Short status
			int limitedNumBoosters = Jobs.Limited().NumBoosters();
			int limitedGemsNeeded = Jobs.Limited().GemsNeeded();
			if (shortStatus) {
				return Commands.FormatBotResponse(Bot, String.Format(Strings.QueueStatusShort, limitedNumBoosters, String.Format("{0:N0}", limitedGemsNeeded), String.Format("~{0:t}", limitedLastBooster.GetAvailableAtTime())));
			}

			// Long status
			List<string> responses = new List<string>();

			// Refreshing booster page
			if (BoosterQueue.IsUpdatingBoosterInfos()) {
				responses.Add(Strings.BoosterInfoUpdating);
			}

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
				responses.Add(String.Format(Strings.QueueStatusOneTimeBoosters, Jobs.Limited().NumCrafted(), limitedNumBoosters, String.Format("~{0:t}", limitedLastBooster.GetAvailableAtTime()), String.Format("{0:N0}", limitedGemsNeeded)));
				responses.Add(String.Format(Strings.QueueStatusOneTimeBoosterList, String.Join(", ", Jobs.Limited().UncraftedGameIDs())));
			}

			// Permanent booster status
			if (Jobs.Permanent().NumBoosters() > 0) {
				responses.Add(String.Format(Strings.QueueStatusPermanentBoosters, String.Format("{0:N0}", Jobs.Permanent().GemsNeeded()), String.Join(", ", Jobs.Permanent().GameIDs())));
			}

			// Next booster to be crafted
			if (DateTime.Now > nextBooster.GetAvailableAtTime()) {
				responses.Add(String.Format(Strings.QueueStatusNextBoosterCraftingNow, nextBooster.Info.Name, nextBooster.GameID));
			} else {
				responses.Add(String.Format(Strings.QueueStatusNextBoosterCraftingLater, String.Format("{0:t}", nextBooster.GetAvailableAtTime()), nextBooster.Info.Name, nextBooster.GameID));
			}

			responses.Add("");

			return Commands.FormatBotResponse(Bot, String.Join(Environment.NewLine, responses));
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
	}
}
