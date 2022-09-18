using System;
using Newtonsoft.Json;

namespace BoosterManager {
	internal sealed class BoosterLastCraft {
		[JsonProperty(Required = Required.Always)]
		internal DateTime CraftTime;

		[JsonProperty(Required = Required.Always)]
		internal int BoosterDelay;

		[JsonConstructor]
		private BoosterLastCraft() { }

		internal BoosterLastCraft(DateTime craftTime, int boosterDelay) {
			CraftTime = craftTime;
			BoosterDelay = boosterDelay;
		}
	}
}
