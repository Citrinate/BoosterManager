using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers;
using ArchiSteamFarm.Helpers.Json;

namespace BoosterManager {
	internal sealed class BoosterDatabase : SerializableFile {
		[JsonInclude]
		private ConcurrentDictionary<uint, BoosterLastCraft> BoosterLastCrafts { get; init; } = new();

		[JsonInclude]
		internal List<BoosterJobState> BoosterJobs { get; private set; } = new();

		[JsonConstructor]
		private BoosterDatabase() { }

		private BoosterDatabase(string filePath) : this() {
			if (string.IsNullOrEmpty(filePath)) {
				throw new ArgumentNullException(nameof(filePath));
			}

			FilePath = filePath;
		}

		protected override Task Save() => Save(this);

		internal static BoosterDatabase CreateOrLoad(string filePath) {
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
					ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorIsEmpty, nameof(json)));

					return new BoosterDatabase(filePath);
				}

				boosterDatabase = json.ToJsonObject<BoosterDatabase>();
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);

				return new BoosterDatabase(filePath);
			}

			if (boosterDatabase == null) {
				ASF.ArchiLogger.LogNullError(boosterDatabase);

				return new BoosterDatabase(filePath);
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

		internal void SetLastCraft(uint appID, DateTime craftTime) {
			BoosterLastCraft newCraft = new BoosterLastCraft(craftTime);
			BoosterLastCrafts.AddOrUpdate(appID, newCraft, (key, oldCraft) => {
				oldCraft.CraftTime = craftTime;

				return oldCraft;
			});
			
			Utilities.InBackground(Save);
		}

		internal void UpdateBoosterJobs(List<BoosterJobState> boosterJobs) {
			BoosterJobs = boosterJobs;

			Utilities.InBackground(Save);
		}
	}
}
