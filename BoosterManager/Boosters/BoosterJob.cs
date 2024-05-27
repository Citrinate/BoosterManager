using System;
using System.Collections.Generic;
using System.Linq;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;

// Represents the state of a !booster command

namespace BoosterManager {
	internal sealed class BoosterJob {
		private Bot Bot;
		internal BoosterJobType JobType;
		private List<uint> GameIDsToBooster;
		internal StatusReporter StatusReporter;
		private bool CreatedFromSaveState = false;
		private readonly object LockObject = new();
		private bool JobStopped = false;
		
		private BoosterHandler BoosterHandler => BoosterHandler.BoosterHandlers[Bot.BotName];
		private BoosterQueue BoosterQueue => BoosterHandler.BoosterQueue;

		private readonly List<Booster> Boosters = new();
		internal bool IsFinished => UncraftedGameIDs.Count == 0;

		internal List<uint> GameIDs {
			get {
				lock(LockObject) {
					return GameIDsToBooster.Concat(Boosters.Select(booster => booster.GameID)).ToList<uint>();
				}
			}
		}

		internal List<uint> UncraftedGameIDs {
			get {
				lock(LockObject) {
					return GameIDsToBooster.Concat(Boosters.Where(booster => !booster.WasCrafted).Select(booster => booster.GameID)).ToList<uint>();
				}
			}
		}

		internal (List<Booster>, List<uint>) QueuedAndUnqueuedBoosters {
			get {
				lock(LockObject) {
					return (Boosters.Where(booster => !booster.WasCrafted).ToList(), GameIDsToBooster);
				}
			}
		}

		internal int NumBoosters {
			get {
				lock(LockObject) {
					return Boosters.Count + GameIDsToBooster.Count;
				}
			}
		}

		internal int NumCrafted => Boosters.Where(booster => booster.WasCrafted).Count();
		internal int NumUncrafted {
			get {
				lock(LockObject) {
					return GameIDsToBooster.Count + Boosters.Where(booster => !booster.WasCrafted).Count();
				}
			}
		}

		internal int GemsNeeded {
			get {
				lock(LockObject) {
					int unqueuedGemsNeeded = 0;
					if (GameIDsToBooster.Count > 0) {
						foreach (var group in GameIDsToBooster.GroupBy(x => x)) {
							uint gameID = group.Key;
							int count = group.Count();
							Booster? booster = BoosterHandler.Jobs.GetBooster(gameID);
							if (booster == null) {
								continue;
							}

							unqueuedGemsNeeded += (int) booster.Info.Price * count;
						}
					}

					return unqueuedGemsNeeded + Boosters.Where(booster => !booster.WasCrafted).Sum(booster => (int) booster.Info.Price);
				}
			}
		}
		internal Booster? NextBooster => Boosters.Where(booster => !booster.WasCrafted).OrderBy(booster => booster.GetAvailableAtTime()).FirstOrDefault(); // Not necessary to consider unqueued boosters here, based on how this property is currently used
		internal DateTime? LastBoosterCraftTime {
			get {
				lock(LockObject) {
					DateTime? lastUnqueuedBoosterCraftTime = null;
					if (GameIDsToBooster.Count > 0) {
						foreach (uint gameID in GameIDsToBooster.Distinct()) {
							int count = BoosterHandler.Jobs.GetNumUnqueuedBoosters(gameID); // Number of unqueable boosters across all jobs for this gameID
							Booster? booster = BoosterHandler.Jobs.GetBooster(gameID); // The one queued booster across all jobs for this gameID
							if (booster == null) {
								continue;
							}

							// I don't consider here if multiple jobs have the same unqueued booster, which will get to queue first
							// It's not relevant to consider this for how this property is currently being used
							lastUnqueuedBoosterCraftTime = BoosterJobUtilities.MaxDateTime(lastUnqueuedBoosterCraftTime, booster.GetAvailableAtTime().AddDays(count));
						}
					}

					Booster? lastQueuedBooster = Boosters.Where(booster => !booster.WasCrafted).OrderBy(booster => booster.GetAvailableAtTime()).LastOrDefault();

					return BoosterJobUtilities.MaxDateTime(lastQueuedBooster?.GetAvailableAtTime(), lastUnqueuedBoosterCraftTime);
				}
			}
		}

		internal BoosterJob(Bot bot, BoosterJobType jobType, List<uint> gameIDsToBooster, StatusReporter statusReporter) {
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

			BoosterQueue.OnBoosterInfosUpdated += OnBoosterInfosUpdated;
			BoosterQueue.OnBoosterRemoved += OnBoosterRemoved;
			BoosterQueue.Start();
		}

		internal void Finish() {
			BoosterQueue.OnBoosterRemoved -= OnBoosterRemoved;

			if (NumBoosters > 0) {
				StatusReporter.Report(Bot, String.Format(Strings.BoosterCreationFinished, NumBoosters));
			}
		}

		internal void Stop() {
			JobStopped = true;
			BoosterQueue.OnBoosterInfosUpdated -= OnBoosterInfosUpdated;
			BoosterQueue.OnBoosterRemoved -= OnBoosterRemoved;

			lock (LockObject) {
				foreach (Booster booster in Boosters.Where(booster => !booster.WasCrafted)) {
					BoosterQueue.RemoveBooster(booster.GameID, BoosterDequeueReason.JobStopped);
				}
			}
		}

		private void SaveJobState() {
			if (JobStopped) {
				return;
			}

			// Save the current state of this job
			BoosterHandler.UpdateBoosterJobs();
		}

		void OnBoosterInfosUpdated(Dictionary<uint, Steam.BoosterInfo> boosterInfos) {
			try {
				// At this point, all boosters that can be added to the queue have been
				if (NumBoosters == 0) {
					StatusReporter.Report(Bot, Strings.BoostersUncraftable, log: CreatedFromSaveState);
					Finish();

					return;
				}

				SaveJobState();

				DateTime? lastBoosterCraftTime = LastBoosterCraftTime;
				if (lastBoosterCraftTime == null) {
					StatusReporter.Report(Bot, String.Format(Strings.QueueStatusShortWithoutTime, NumBoosters, String.Format("{0:N0}", GemsNeeded)), log: CreatedFromSaveState);
				} else if (lastBoosterCraftTime.Value.Date == DateTime.Today) {
					StatusReporter.Report(Bot, String.Format(Strings.QueueStatusShort, NumBoosters, String.Format("{0:N0}", GemsNeeded), String.Format("{0:t}", lastBoosterCraftTime)), log: CreatedFromSaveState);
				} else {
					StatusReporter.Report(Bot, String.Format(Strings.QueueStatusShortWithDate, NumBoosters, String.Format("{0:N0}", GemsNeeded), String.Format("{0:d}", lastBoosterCraftTime), String.Format("{0:t}", lastBoosterCraftTime)), log: CreatedFromSaveState);
				}
			} finally {
				BoosterQueue.OnBoosterInfosUpdated -= OnBoosterInfosUpdated;
			}
		}

		internal void OnBoosterRemoved(Booster booster, BoosterDequeueReason reason) {
			if (!(reason == BoosterDequeueReason.Crafted 
				// Currently we don't prevent user from queing a booster that already exists in the permanent booster job
				// This can prevent the permanent job from queueing boosters if the user sometimes removes a booster, this addresses that
				|| (JobType == BoosterJobType.Permanent && reason == BoosterDequeueReason.RemovedByUser)
			)) {
				return;
			}

			lock(LockObject) {
				if (GameIDsToBooster.Contains(booster.GameID)) {
					// Try to queue boosters that couldn't initially be queued
					BoosterQueue.AddBooster(booster.GameID, this);
					BoosterQueue.Start();
				}
			}
		}

		internal void OnBoosterQueued(Booster booster) {
			lock(LockObject) {
				if (!GameIDsToBooster.Remove(booster.GameID)) {
					// We queued a booster that no longer exists in our list of boosters to craft, must have been removed by the user
					BoosterQueue.RemoveBooster(booster.GameID, BoosterDequeueReason.RemovedByUser);
				}

				Boosters.Add(booster);				
			}
		}

		internal void OnBoosterUnqueueable (uint gameID, BoosterDequeueReason reason) {
			if (reason == BoosterDequeueReason.AlreadyQueued) {
				// We'll try again later
				return;
			}

			// All other reasons are some variation of "we can't craft this booster"
			lock(LockObject) {
				GameIDsToBooster.RemoveAll(x => x == gameID);
			}
			
			SaveJobState();
		}

		internal void OnBoosterDequeued(Booster booster, BoosterDequeueReason reason) {
			if (reason == BoosterDequeueReason.UnexpectedlyUncraftable) {
				// No longer have access to craft boosters for this game (game removed from account, or sometimes due to very rare Steam bugs)
				lock(LockObject) {
					Boosters.Remove(booster);
				}
				StatusReporter.Report(Bot, String.Format(Strings.BoosterUnexpectedlyUncraftable, booster.Info.Name, booster.GameID));
				SaveJobState();

				return;
			}

			if (JobType == BoosterJobType.Permanent) {
				// Requeue this booster as permanent boosters are meant to be crafted endlessly
				lock(LockObject) {
					Boosters.Remove(booster);
					GameIDsToBooster.Add(booster.GameID);
				}

				Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.PermanentBoosterRequeued, booster.GameID));
				BoosterQueue.AddBooster(booster.GameID, this);
				BoosterQueue.Start();

				return;
			}

			if (JobType == BoosterJobType.Limited) {
				if (reason == BoosterDequeueReason.RemovedByUser) {
					lock(LockObject) {
						Boosters.Remove(booster);
					}
				}

				SaveJobState();

				return;
			}
		}

		internal void OnInsufficientGems(Booster booster) {
			StatusReporter.Report(Bot, String.Format(Strings.NotEnoughGems, String.Format("{0:N0}", booster.Info.Price - BoosterQueue.AvailableGems)), suppressDuplicateMessages: true);
		}

		internal int RemoveBoosters(uint gameID) {
			if (JobType == BoosterJobType.Permanent) {
				return 0;
			}

			int numRemoved = 0;

			lock(LockObject) {
				numRemoved += GameIDsToBooster.RemoveAll(x => x == gameID);

				if (BoosterQueue.RemoveBooster(gameID, BoosterDequeueReason.RemovedByUser, this)) {
					numRemoved++;
				}
			}

			if (numRemoved > 0) {
				for (int i = 0; i < numRemoved; i++) {
					Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterUnqueuedByUser, gameID));
				}
			}

			return numRemoved;
		}

		internal bool RemoveUnqueuedBooster(uint gameID) {
			if (JobType == BoosterJobType.Permanent) {
				return false;
			}

			bool removed = false;

			lock(LockObject) {
				removed = GameIDsToBooster.Remove(gameID);
			}

			SaveJobState();

			return removed;
		}

		internal bool RemoveQueuedBooster(uint gameID) {
			if (JobType == BoosterJobType.Permanent) {
				return false;
			}

			lock(LockObject) {
				return BoosterQueue.RemoveBooster(gameID, BoosterDequeueReason.RemovedByUser, this);
			}
		}

		internal List<uint> RemoveAllBoosters() {
			if (JobType == BoosterJobType.Permanent) {
				return new List<uint>();
			}

			List<uint> gameIDsRemoved = new List<uint>();

			lock(LockObject) {
				foreach (uint gameID in GameIDsToBooster.ToList()) {
					if (GameIDsToBooster.Remove(gameID)) {
						gameIDsRemoved.Add(gameID);
					}
				}

				foreach (Booster booster in Boosters.ToList()) {
					if (BoosterQueue.RemoveBooster(booster.GameID, BoosterDequeueReason.RemovedByUser)) {
						gameIDsRemoved.Add(booster.GameID);
					}
				}
			}

			SaveJobState();

			return gameIDsRemoved;
		}

		internal Booster? GetBooster(uint gameID) {
			lock(LockObject) {
				return Boosters.FirstOrDefault(booster => !booster.WasCrafted && booster.GameID == gameID);
			}
		}

		internal int GetNumUnqueuedBoosters(uint gameID) {
			lock(LockObject) {
				return GameIDsToBooster.Where(x => x == gameID).Count();
			}
		}

		internal int GetNumBoosters(uint gameID) {
			lock(LockObject) {
				return (GetBooster(gameID) == null ? 0 : 1) + GetNumUnqueuedBoosters(gameID);
			}
		}
	}
}
