using System;
using System.Collections.Concurrent;
using System.IO;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers;
using ArchiSteamFarm.Localization;
using Newtonsoft.Json;

namespace BoosterManager {
	internal sealed class BoosterDatabase : SerializableFile {
		[JsonProperty(Required = Required.DisallowNull)]
		private readonly ConcurrentDictionary<uint, BoosterLastCraft> BoosterLastCrafts = new();

		[JsonConstructor]
		private BoosterDatabase() { }

		private BoosterDatabase(string filePath) : this() {
			if (string.IsNullOrEmpty(filePath)) {
				throw new ArgumentNullException(nameof(filePath));
			}

			FilePath = filePath;
		}

		internal static BoosterDatabase? CreateOrLoad(string filePath) {
			if (string.IsNullOrEmpty(filePath)) {
				throw new ArgumentNullException(nameof(filePath));
			}

			if (!File.Exists(filePath)) {
				return new BoosterDatabase(filePath);
			}

			BoosterDatabase? boosterDatabase;

			try {
				string json = File.ReadAllText(filePath);

				if (string.IsNullOrEmpty(json)) {
					ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorIsEmpty, nameof(json)));

					return null;
				}

				boosterDatabase = JsonConvert.DeserializeObject<BoosterDatabase>(json);
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);

				return null;
			}

			if (boosterDatabase == null) {
				ASF.ArchiLogger.LogNullError(boosterDatabase);

				return null;
			}

			boosterDatabase.FilePath = filePath;

			return boosterDatabase;
		}

		internal BoosterLastCraft? GetLastCraft(uint appID) {
			if (BoosterLastCrafts.TryGetValue(appID, out BoosterLastCraft? lastCraft)) {
				if ((DateTime.Now - lastCraft.CraftTime).TotalHours >= 24) {
					// Stored data is too old, delete it
					BoosterLastCrafts.TryRemove(appID, out _);
					Utilities.InBackground(Save);

					return null;
				}

				return lastCraft;
			}

			return null;
		}

		internal void SetLastCraft(uint appID, DateTime craftTime, int boosterDelay) {
			BoosterLastCraft newCraft = new BoosterLastCraft(craftTime, boosterDelay);
			BoosterLastCrafts.AddOrUpdate(appID, newCraft, (key, oldCraft) => {
				oldCraft.CraftTime = craftTime;
				// boosterDelay might change, but the old delay will still be there, the real delay will be the bigger of the two
				oldCraft.BoosterDelay = Math.Max(oldCraft.BoosterDelay, boosterDelay);

				return oldCraft;
			});
			Utilities.InBackground(Save);
		}
	}
}
