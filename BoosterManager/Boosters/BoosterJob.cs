using System;
using System.Collections.Generic;
using System.Linq;
using ArchiSteamFarm.Collections;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;

// Represents the state of a !booster command

namespace BoosterManager {
	internal sealed class BoosterJob {
		private Bot Bot;
		internal BoosterJobType JobType;
		private HashSet<uint> GameIDsToBooster;
		internal StatusReporter StatusReporter;
		private bool CreatedFromSaveState = false;
		private readonly object LockObject = new();
		
		private BoosterHandler BoosterHandler => BoosterHandler.BoosterHandlers[Bot.BotName];
		private BoosterQueue BoosterQueue => BoosterHandler.BoosterQueue;

		private readonly ConcurrentHashSet<Booster> Boosters = new(new BoosterComparer());
		internal bool IsFinished {
			get {
				lock(LockObject) {
					return GameIDsToBooster.Count == 0 && UncraftedGameIDs.Count == 0;
				}
			}
		}

		internal HashSet<uint> GameIDs {
			get {
				lock(LockObject) {
					return GameIDsToBooster.Union(Boosters.Select(booster => booster.GameID)).ToHashSet<uint>();
				}
			}
		}

		internal HashSet<uint> UncraftedGameIDs {
			get {
				lock(LockObject) {
					return GameIDsToBooster.Union(Boosters.Where(booster => !booster.WasCrafted).Select(booster => booster.GameID)).ToHashSet<uint>();
				}
			}
		}

		internal int NumBoosters => Boosters.Count;
		internal int NumCrafted => Boosters.Where(booster => booster.WasCrafted).Count();
		internal int NumUncrafted => Boosters.Where(booster => !booster.WasCrafted).Count();
		internal int GemsNeeded => Boosters.Where(booster => !booster.WasCrafted).Sum(booster => (int) booster.Info.Price);
		internal Booster? NextBooster => Boosters.Where(booster => !booster.WasCrafted).OrderBy(booster => booster.GetAvailableAtTime()).FirstOrDefault();
		internal Booster? LastBooster => Boosters.Where(booster => !booster.WasCrafted).OrderBy(booster => booster.GetAvailableAtTime()).LastOrDefault();

		internal BoosterJob(Bot bot, BoosterJobType jobType, HashSet<uint> gameIDsToBooster, StatusReporter statusReporter) {
			Bot = bot;
			JobType = jobType;
			StatusReporter = statusReporter;
			GameIDsToBooster = gameIDsToBooster;
			
			Start();
		}

		internal BoosterJob(Bot bot, BoosterJobType jobType, BoosterJobState jobState) : this(bot, jobType, jobState.GameIDs, jobState.StatusReporter) {
			CreatedFromSaveState = true;
		}

		private void Start() {
			foreach (uint gameID in GameIDsToBooster) {
				BoosterQueue.AddBooster(gameID, this);
			}

			// void OnBoosterInfosUpdated() {
			void OnBoosterInfosUpdated(Dictionary<uint, Steam.BoosterInfo> boosterInfos) {
				try {
					Booster? lastBooster = LastBooster;
					if (lastBooster == null) {
						StatusReporter.Report(Bot, Strings.BoostersUncraftable, log: CreatedFromSaveState);
						Finish();

						return;
					}

					BoosterHandler.UpdateBoosterJobs();
					StatusReporter.Report(Bot, String.Format(Strings.QueueStatusShort, NumBoosters, String.Format("{0:N0}", GemsNeeded), String.Format("~{0:t}", lastBooster.GetAvailableAtTime())), log: CreatedFromSaveState);
				} finally {
					BoosterQueue.OnBoosterInfosUpdated -= OnBoosterInfosUpdated;
				}
			}

			BoosterQueue.OnBoosterInfosUpdated += OnBoosterInfosUpdated;
			BoosterQueue.Start();
		}

		internal void OnBoosterQueued(Booster booster) {
			lock(LockObject) {
				if (!GameIDsToBooster.Remove(booster.GameID)) {
					BoosterQueue.RemoveBooster(booster.GameID, BoosterDequeueReason.RemovedByUser);
				}

				Boosters.Add(booster);				
			}
		}

		internal void OnBoosterUnqueueable (uint gameID, BoosterDequeueReason reason) {
			GameIDsToBooster.Remove(gameID);
			CheckIfFinished();
		}

		internal void OnBoosterDequeued(Booster booster, BoosterDequeueReason reason) {
			if (reason == BoosterDequeueReason.UnexpectedlyUncraftable) {
				// No longer have access to craft boosters for this game (game removed from account, or sometimes due to very rare Steam bugs)
				Boosters.Remove(booster);
				StatusReporter.Report(Bot, String.Format(Strings.BoosterUnexpectedlyUncraftable, booster.Info.Name, booster.GameID));
				CheckIfFinished();

				return;
			}

			if (JobType == BoosterJobType.Permanent) {
				lock(LockObject) {
					Boosters.Remove(booster);
					GameIDsToBooster.Add(booster.GameID);
				}

				Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.PermanentBoosterRequeued, booster.GameID));
				BoosterQueue.AddBooster(booster.GameID, this);

				return;
			}

			if (JobType == BoosterJobType.Limited) {
				if (reason == BoosterDequeueReason.RemovedByUser) {
					Boosters.Remove(booster);
				}

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
					HashSet<uint> timeFilteredGameIDs = Boosters.Where(booster => !booster.WasCrafted && booster.GetAvailableAtTime() > timeLimit).Select(booster => booster.GameID).ToHashSet<uint>();
					gameIDs.UnionWith(timeFilteredGameIDs);
				}
			}

			HashSet<uint> removedGameIDs = new HashSet<uint>();
			foreach (uint gameID in gameIDs) {
				bool removed = false;

				if (GameIDsToBooster.Remove(gameID)) {
					removed = true;
				}

				if (BoosterQueue.RemoveBooster(gameID, BoosterDequeueReason.RemovedByUser)) {
					removed = true;
				}
				
				if (removed) {
					Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterUnqueuedByUser, gameID));
					removedGameIDs.Add(gameID);
				}
			}

			return removedGameIDs;
		}

		private void CheckIfFinished() {
			if (!IsFinished) {
				BoosterHandler.UpdateBoosterJobs();

				return;
			}

			Finish();
		}

		private void Finish() {
			if (NumBoosters > 0) {
				StatusReporter.Report(Bot, String.Format(Strings.BoosterCreationFinished, NumBoosters));
			}

			BoosterHandler.UpdateBoosterJobs();
		}
	}
}
