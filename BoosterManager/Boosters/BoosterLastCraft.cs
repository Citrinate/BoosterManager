using System;
using System.Text.Json.Serialization;

namespace BoosterManager {
	internal sealed class BoosterLastCraft {
		[JsonInclude]
		[JsonRequired]
		internal DateTime CraftTime { get; set; }

		[JsonInclude]
		[JsonRequired]
		internal int BoosterDelay { get; set; }

		[JsonConstructor]
		private BoosterLastCraft() { }

		internal BoosterLastCraft(DateTime craftTime, int boosterDelay) {
			CraftTime = craftTime;
			BoosterDelay = boosterDelay;
		}
	}
}
