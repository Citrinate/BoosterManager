using System.Collections.Generic;
using System.Linq;

namespace BoosterManager {
	internal static class BoosterJobUtilities {
		internal static IEnumerable<BoosterJob> Limited(this IEnumerable<BoosterJob> jobs) {
			return jobs.Where(job => job.JobType == BoosterJobType.Limited);
		}

		internal static IEnumerable<BoosterJob> Permanent(this IEnumerable<BoosterJob> jobs) {
			return jobs.Where(job => job.JobType == BoosterJobType.Permanent);
		}

		internal static IEnumerable<BoosterJob> Finished(this IEnumerable<BoosterJob> jobs) {
			return jobs.Where(job => job.IsFinished);
		}

		internal static IEnumerable<BoosterJob> Unfinised(this IEnumerable<BoosterJob> jobs) {
			return jobs.Where(job => !job.IsFinished);
		}

		internal static List<BoosterJobState> SaveState (this IEnumerable<BoosterJob> jobs) {
			return jobs.Select(job => new BoosterJobState(job)).ToList();
		}

		internal static HashSet<uint> GameIDs (this IEnumerable<BoosterJob> jobs) {
			HashSet<uint> gameIDs = new();
			foreach (HashSet<uint> gameIDsFromJob in jobs.Select(job => job.GameIDs)) {
				gameIDs.UnionWith(gameIDsFromJob);
			}

			return gameIDs;
		}

		internal static HashSet<uint> UncraftedGameIDs (this IEnumerable<BoosterJob> jobs) {
			HashSet<uint> uncraftedGameIDs = new();
			foreach (HashSet<uint> uncraftedGameIDsFromJob in jobs.Select(job => job.UncraftedGameIDs)) {
				uncraftedGameIDs.UnionWith(uncraftedGameIDsFromJob);
			}

			return uncraftedGameIDs;
		}

		internal static int NumBoosters (this IEnumerable<BoosterJob> jobs) {
			return jobs.Sum(job => job.NumBoosters);
		}

		internal static int NumCrafted (this IEnumerable<BoosterJob> jobs) {
			return jobs.Sum(job => job.NumCrafted);
		}

		internal static int NumUncrafted (this IEnumerable<BoosterJob> jobs) {
			return jobs.Sum(job => job.NumUncrafted);
		}

		internal static int GemsNeeded (this IEnumerable<BoosterJob> jobs) {
			return jobs.Sum(job => job.GemsNeeded);
		}

		internal static Booster? NextBooster (this IEnumerable<BoosterJob> jobs) {
			List<Booster> boosters = jobs.Select(job => job.NextBooster).Where(booster => booster != null)!.ToList<Booster>();
			if (boosters.Count == 0) {
				return null;
			}

			return boosters.OrderBy(booster => booster.GetAvailableAtTime()).First();
		}

		internal static Booster? LastBooster (this IEnumerable<BoosterJob> jobs) {
			List<Booster> boosters = jobs.Select(job => job.LastBooster).Where(booster => booster != null)!.ToList<Booster>();
			if (boosters.Count == 0) {
				return null;
			}

			return boosters.OrderBy(booster => booster.GetAvailableAtTime()).Last();
		}

		internal static HashSet<uint> RemoveBoosters (this IEnumerable<BoosterJob> jobs, HashSet<uint>? gameIDs = null, int? timeLimitHours = null) {
			HashSet<uint> removedBoosters = new();
			foreach (HashSet<uint> removedFromJob in jobs.ToList().Select(job => job.RemoveBoosters(gameIDs, timeLimitHours))) {
				removedBoosters.UnionWith(removedFromJob);
			}

			return removedBoosters;
		}
	}
}
