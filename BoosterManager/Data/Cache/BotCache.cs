using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ArchiSteamFarm.Collections;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers;
using ArchiSteamFarm.Helpers.Json;

namespace BoosterManager {
	internal sealed class BotCache : SerializableFile {
		[JsonInclude]
		private ConcurrentDictionary<uint, BoosterLastCraft> BoosterLastCrafts { get; init; } = new();

		[JsonInclude]
		internal List<BoosterJobState> BoosterJobs { get; private set; } = new();

		[JsonInclude]
		internal uint? CraftingGameID { get; private set; } = null;

		[JsonInclude]
		internal DateTime? CraftingTime { get; private set; } = null;
		
		[JsonInclude]
		internal ConcurrentHashSet<MarketAlert> MarketAlerts { get; init; } = new();

		[JsonConstructor]
		private BotCache() { }

		private BotCache(string filePath) : this() {
			if (string.IsNullOrEmpty(filePath)) {
				throw new ArgumentNullException(nameof(filePath));
			}

			FilePath = filePath;
		}

		protected override Task Save() => Save(this);

		internal static BotCache CreateOrLoad(string filePath) {
			if (string.IsNullOrEmpty(filePath)) {
				throw new ArgumentNullException(nameof(filePath));
			}

			if (!File.Exists(filePath)) {
				return new BotCache(filePath);
			}

			BotCache? botCache;

			try {
				string json = File.ReadAllText(filePath);

				if (string.IsNullOrEmpty(json)) {
					ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorIsEmpty, nameof(json)));

					return new BotCache(filePath);
				}

				botCache = json.ToJsonObject<BotCache>();
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);

				return new BotCache(filePath);
			}

			if (botCache == null) {
				ASF.ArchiLogger.LogNullError(botCache);

				return new BotCache(filePath);
			}

			botCache.FilePath = filePath;

			return botCache;
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
			if (!BoosterLastCrafts.TryAdd(appID, new BoosterLastCraft(craftTime))) {
				BoosterLastCrafts[appID].CraftTime = craftTime;
			}
			
			Utilities.InBackground(Save);
		}

		internal void UpdateBoosterJobs(List<BoosterJobState> boosterJobs) {
			BoosterJobs = boosterJobs;

			Utilities.InBackground(Save);
		}

		internal async Task PreCraft(Booster booster) {
			CraftingGameID = booster.GameID;
			CraftingTime = booster.Info.AvailableAtTime;

			await Save().ConfigureAwait(false);
		}

		internal void PostCraft() {
			CraftingGameID = null;
			CraftingTime = null;

			Utilities.InBackground(Save);
		}

		internal bool AddMarketAlert(MarketAlert alert) {
			bool exists = MarketAlerts.FirstOrDefault(x => new MarketAlertComparer().Equals(alert, x)) != null;
			if (exists) {
				return false;
			}

			if (MarketAlerts.Add(alert)) {
				Utilities.InBackground(Save);

				return true;
			}

			return false;
		}

		internal void RemoveMarketAlert(MarketAlert alert) {
			MarketAlerts.Remove(alert);

			Utilities.InBackground(Save);
		}
	}
}
