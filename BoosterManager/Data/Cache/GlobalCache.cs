using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers;
using ArchiSteamFarm.Helpers.Json;

namespace BoosterManager {
	internal sealed class GlobalCache : SerializableFile {
		private static string SharedFilePath => Path.Combine(ArchiSteamFarm.SharedInfo.ConfigDirectory, $"{nameof(BoosterManager)}.cache");

		[JsonInclude]
		private ConcurrentDictionary<uint, ConcurrentDictionary<string, uint>> NameIDs { get; init; } = new();

		[JsonConstructor]
		internal GlobalCache() {
			FilePath = SharedFilePath;
		}

		protected override Task Save() => Save(this);

		internal static async Task<GlobalCache> CreateOrLoad() {
			if (!File.Exists(SharedFilePath)) {
				return new GlobalCache();
			}

			GlobalCache? globalCache;
			try {
				string json = await File.ReadAllTextAsync(SharedFilePath).ConfigureAwait(false);

				if (string.IsNullOrEmpty(json)) {
					ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorIsEmpty, nameof(json)));

					return new GlobalCache();
				}

				globalCache = json.ToJsonObject<GlobalCache>();
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);

				return new GlobalCache();
			}

			if (globalCache == null) {
				ASF.ArchiLogger.LogNullError(globalCache);

				return new GlobalCache();
			}
			
			return globalCache;
		}

		internal void SetNameID(uint appID, string hashName, uint nameID) {
			NameIDs.TryAdd(appID, new());
			NameIDs[appID][hashName] = nameID;
			
			Utilities.InBackground(Save);
		}

		internal bool TryGetNameID(uint appID, string hashName, out uint nameID) {
			if (NameIDs.TryGetValue(appID, out ConcurrentDictionary<string, uint>? dictionary)) {
				if (dictionary.TryGetValue(hashName, out uint outValue)) {
					nameID = outValue;
					return true;
				}
			}

			nameID = 0;
			return false;
		}
	}
}
