using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;

// Represents the state of a !booster command

namespace BoosterManager {
	internal sealed class BoosterJob {
		private Bot Bot;
		internal BoosterJobType JobType;
		private HashSet<uint> GameIDsToBooster;
		private StatusReporter StatusReporter;
		internal bool IsFinished = false;
		
		private BoosterHandler BoosterHandler => BoosterHandler.BoosterHandlers[Bot.BotName];
		private BoosterQueue BoosterQueue => BoosterHandler.BoosterHandlers[Bot.BotName].BoosterQueue;

		private readonly ConcurrentDictionary<uint, Booster> Boosters = new();
		internal HashSet<uint> GameIDs => Boosters.Keys.ToHashSet<uint>();
		internal HashSet<uint> UncraftedGameIDs => Boosters.Values.Where(booster => !booster.WasCrafted).Select(booster => booster.Info.AppID).ToHashSet<uint>();
		internal int NumBoosters => Boosters.Count;
		internal int NumCrafted => Boosters.Values.Where(booster => booster.WasCrafted).Count();
		internal int NumUncrafted => Boosters.Values.Where(booster => !booster.WasCrafted).Count();
		internal int GemsNeeded => Boosters.Values.Where(booster => !booster.WasCrafted).Sum(booster => (int) booster.Info.Price);
		internal Booster? NextBooster => Boosters.Values.Where(booster => !booster.WasCrafted).OrderBy(booster => booster.GetAvailableAtTime()).FirstOrDefault();
		internal Booster? LastBooster => Boosters.Values.Where(booster => !booster.WasCrafted).OrderBy(booster => booster.GetAvailableAtTime()).LastOrDefault();

		internal BoosterJob(Bot bot, BoosterJobType jobType, HashSet<uint> gameIDsToBooster, StatusReporter statusReporter) {
			Bot = bot;
			JobType = jobType;
			StatusReporter = statusReporter;
			GameIDsToBooster = gameIDsToBooster;

			Start();
		}

		private void Start() {
			foreach (uint gameID in GameIDsToBooster) {
				BoosterQueue.AddBooster(gameID, this);
			}

			void handler() {
				try {
					Booster? lastBooster = LastBooster;
					if (lastBooster == null) {
						StatusReporter.Report(Bot, Strings.BoostersUncraftable);

						return;
					}

					StatusReporter.Report(Bot, String.Format(Strings.QueueStatusShort, NumBoosters, String.Format("{0:N0}", GemsNeeded), String.Format("~{0:t}", lastBooster.GetAvailableAtTime())));
				} finally {
					BoosterQueue.OnBoosterInfosUpdated -= handler;
				}
			}

			BoosterQueue.OnBoosterInfosUpdated += handler;
			BoosterQueue.Start();
		}

		internal void OnBoosterQueued(Booster booster) {
			Boosters.TryAdd(booster.GameID, booster);
		}

		internal void OnBoosterUnqueueable (uint gameID) {
			GameIDsToBooster.Remove(gameID);
			CheckIfFinished();
		}

		internal void OnBoosterDequeued(Booster booster, BoosterDequeueReason reason) {
			if (reason == BoosterDequeueReason.UnexpectedlyUncraftable) {
				// No longer have access to craft boosters for this game (game removed from account, or sometimes due to very rare Steam bugs)
				Boosters.TryRemove(booster.GameID, out Booster? _);
				GameIDsToBooster.Remove(booster.GameID);
				StatusReporter.Report(Bot, String.Format(Strings.BoosterUnexpectedlyUncraftable, booster.Info.Name, booster.GameID));
				CheckIfFinished();

				return;
			}

			if (JobType == BoosterJobType.Permanent) {
				Boosters.TryRemove(booster.GameID, out Booster? _);
				Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.PermanentBoosterRequeued, booster.GameID));
				BoosterQueue.AddBooster(booster.GameID, this);

				return;
			}

			if (JobType == BoosterJobType.Limited) {
				if (reason == BoosterDequeueReason.RemovedByUser) {
					Boosters.TryRemove(booster.GameID, out Booster? _);
				}

				GameIDsToBooster.Remove(booster.GameID);
				CheckIfFinished();

				return;
			}
		}

		internal void OnInsufficientGems(Booster booster) {
			StatusReporter.Report(Bot, String.Format(Strings.NotEnoughGems, String.Format("{0:N0}", booster.Info.Price - BoosterQueue.AvailableGems)), suppressDuplicateMessages: true);
		}

		internal HashSet<uint> RemoveBoosters(HashSet<uint>? gameIDs = null, int? timeLimitHours = null) {
			if (JobType == BoosterJobType.Permanent) {
				return new HashSet<uint>();
			}

			if (gameIDs == null) {
				gameIDs = new HashSet<uint>();
			}

			if (timeLimitHours != null) {
				// Remove everything that will take more than a certain number of hours to craft
				if (timeLimitHours == 0) {
					// Cancel everything, as everything takes more than 0 hours to craft
					gameIDs.UnionWith(GameIDs);
				} else {
					DateTime timeLimit = DateTime.Now.AddHours(timeLimitHours.Value);
					HashSet<uint> timeFilteredGameIDs = Boosters.Values.Where(booster => !booster.WasCrafted && booster.GetAvailableAtTime() > timeLimit).Select(booster => booster.GameID).ToHashSet<uint>();
					gameIDs.UnionWith(timeFilteredGameIDs);
				}
			}

			HashSet<uint> removedGameIDs = new HashSet<uint>();
			foreach (uint gameID in gameIDs) {
				if (BoosterQueue.RemoveBooster(gameID, BoosterDequeueReason.RemovedByUser)) {
					Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterUnqueuedByUser, gameID));
					removedGameIDs.Add(gameID);
				}
			}

			return removedGameIDs;
		}

		private void CheckIfFinished() {
			if (GameIDsToBooster.Count > 0) {
				return;
			}

			Finish();
		}

		private void Finish() {
			IsFinished = true;
			BoosterHandler.UpdateJobs();

			if (NumBoosters > 0) {
				StatusReporter.Report(Bot, String.Format(Strings.BoosterCreationFinished, NumBoosters));
			}
		}
	}
}
