using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Localization;

namespace BoosterManager {
	internal sealed class BoosterDatabase : SerializableFile {
		[JsonInclude]
		private ConcurrentDictionary<uint, BoosterLastCraft> BoosterLastCrafts { get; init; } = new();

		[JsonConstructor]
		private BoosterDatabase() { }

		private BoosterDatabase(string filePath) : this() {
			if (string.IsNullOrEmpty(filePath)) {
				throw new ArgumentNullException(nameof(filePath));
			}

			FilePath = filePath;
		}

		protected override Task Save() => Save(this);

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

				boosterDatabase = json.ToJsonObject<BoosterDatabase>();
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
