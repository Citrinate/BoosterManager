using System;
using System.Collections.Generic;
using System.Linq;

namespace BoosterManager {
	internal static class BoosterJobUtilities {
		internal static IEnumerable<BoosterJob> Limited(this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Where(job => job.JobType == BoosterJobType.Limited);
		}

		internal static IEnumerable<BoosterJob> Permanent(this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Where(job => job.JobType == BoosterJobType.Permanent);
		}

		internal static IEnumerable<BoosterJob> Finished(this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Where(job => job.IsFinished);
		}

		internal static IEnumerable<BoosterJob> Unfinised(this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Where(job => !job.IsFinished);
		}

		internal static List<BoosterJobState> SaveState (this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Select(job => new BoosterJobState(job)).ToList();
		}

		internal static List<uint> GameIDs (this IEnumerable<BoosterJob> jobs) {
			List<uint> gameIDs = new();
			foreach (List<uint> gameIDsFromJob in jobs.ToList().Select(job => job.GameIDs)) {
				gameIDs.AddRange(gameIDsFromJob);
			}

			return gameIDs;
		}

		internal static List<uint> UncraftedGameIDs (this IEnumerable<BoosterJob> jobs) {
			List<uint> uncraftedGameIDs = new();
			foreach (List<uint> uncraftedGameIDsFromJob in jobs.ToList().Select(job => job.UncraftedGameIDs)) {
				uncraftedGameIDs.AddRange(uncraftedGameIDsFromJob);
			}

			return uncraftedGameIDs;
		}

		internal static (List<Booster>, List<uint>) QueuedAndUnqueuedBoosters (this IEnumerable<BoosterJob> jobs) {
			List<Booster> queuedBoosters = new();
			List<uint> unqueuedBoosters = new();
			foreach ((List<Booster> queuedBoostersFromJob, List<uint> unqueuedBoostersFromJob) in jobs.ToList().Select(job => job.QueuedAndUnqueuedBoosters)) {
				queuedBoosters.AddRange(queuedBoostersFromJob);
				unqueuedBoosters.AddRange(unqueuedBoostersFromJob);
			}

			return (queuedBoosters, unqueuedBoosters);
		}

		internal static int NumBoosters (this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Sum(job => job.NumBoosters);
		}

		internal static int NumCrafted (this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Sum(job => job.NumCrafted);
		}

		internal static int NumUncrafted (this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Sum(job => job.NumUncrafted);
		}

		internal static int GemsNeeded (this IEnumerable<BoosterJob> jobs) {
			return jobs.ToList().Sum(job => job.GemsNeeded);
		}

		internal static Booster? NextBooster (this IEnumerable<BoosterJob> jobs) {
			List<Booster> boosters = jobs.ToList().Select(job => job.NextBooster).Where(booster => booster != null)!.ToList<Booster>();
			if (boosters.Count == 0) {
				return null;
			}

			return boosters.OrderBy(booster => booster.GetAvailableAtTime()).First();
		}

		internal static DateTime? LastBoosterCraftTime (this IEnumerable<BoosterJob> jobs) {
			DateTime? lastBoosterCraftTime = null;
			foreach (BoosterJob job in jobs.ToList()) {
				lastBoosterCraftTime = MaxDateTime(lastBoosterCraftTime, job.LastBoosterCraftTime);
			}

			return lastBoosterCraftTime;
		}

		internal static int RemoveBoosters(this IEnumerable<BoosterJob> jobs, uint gameID) {
			return jobs.ToList().Sum(job => job.RemoveBoosters(gameID));
		}

		internal static bool RemoveUnqueuedBooster(this IEnumerable<BoosterJob> jobs, uint gameID) {
			return jobs.ToList().Any(job => job.RemoveUnqueuedBooster(gameID));
		}

		internal static bool RemoveQueuedBooster(this IEnumerable<BoosterJob> jobs, uint gameID) {
			return jobs.ToList().Any(job => job.RemoveQueuedBooster(gameID));
		}

		internal static List<uint> RemoveAllBoosters(this IEnumerable<BoosterJob> jobs) {
			List<uint> removedGameIDs = new();
			foreach (List<uint> removedFromJob in jobs.ToList().Select(job => job.RemoveAllBoosters())) {
				removedGameIDs.AddRange(removedFromJob);
			}

			return removedGameIDs;
		}

		internal static Booster? GetBooster(this IEnumerable<BoosterJob> jobs, uint gameID) {
			return jobs.ToList().Select(job => job.GetBooster(gameID)).FirstOrDefault();
		}

		internal static int GetNumUnqueuedBoosters(this IEnumerable<BoosterJob> jobs, uint gameID) {
			return jobs.ToList().Sum(job => job.GetNumUnqueuedBoosters(gameID));
		}

		internal static DateTime? MaxDateTime(DateTime? a, DateTime? b) {
			if (a == null || b == null) {
				if (a == null && b == null) {
					return null;
				}

				return a == null ? b : a;
			}

			return a > b ? a : b;
		}
	}
}
