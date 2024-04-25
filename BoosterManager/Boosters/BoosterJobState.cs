using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BoosterManager {
	internal sealed class BoosterJobState {
		[JsonInclude]
		[JsonRequired]
		internal HashSet<uint> GameIDs { get; init; }

		[JsonInclude]
		[JsonRequired]
		internal StatusReporter StatusReporter { get; init; }

		internal BoosterJobState(BoosterJob boosterJob) {
			GameIDs = boosterJob.UncraftedGameIDs;
			StatusReporter = boosterJob.StatusReporter;
		}

		[JsonConstructor]
		internal BoosterJobState(HashSet<uint> gameIDs, StatusReporter statusReporter) {
			GameIDs = gameIDs;
			StatusReporter = statusReporter;
		}
	}
}
