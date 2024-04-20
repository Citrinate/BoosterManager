using System;
using System.Text.Json.Serialization;

namespace BoosterManager {
	internal sealed class BoosterLastCraft {
		[JsonInclude]
		[JsonRequired]
		internal DateTime CraftTime { get; set; }

		[JsonConstructor]
		private BoosterLastCraft() { }

		internal BoosterLastCraft(DateTime craftTime) {
			CraftTime = craftTime;
		}
	}
}
