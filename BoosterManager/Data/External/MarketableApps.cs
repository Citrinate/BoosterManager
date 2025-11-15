using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Web.Responses;
using BoosterManager.Localization;

namespace BoosterManager {
	internal static class MarketableApps {
		internal static HashSet<uint> AppIDs = new();
		
		private static Uri Source = new("https://raw.githubusercontent.com/Citrinate/Steam-MarketableApps/main/data/marketable_apps.min.json");
		private static TimeSpan UpdateFrequency = TimeSpan.FromMinutes(30);

		private static DateTime? LastUpdate;
		private static SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);

		internal static async Task<bool> Update() {
			ArgumentNullException.ThrowIfNull(ASF.WebBrowser);

			await UpdateSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				if (LastUpdate != null && (LastUpdate + UpdateFrequency) > DateTime.Now) {
					return true;
				}

				ObjectResponse<HashSet<uint>>? response = await ASF.WebBrowser.UrlGetToJsonObject<HashSet<uint>>(Source).ConfigureAwait(false);
				if (response == null || response.Content == null) {
					ASF.ArchiLogger.LogGenericDebug(Strings.MarketableAppDataFetchFailed);

					return false;
				}

				AppIDs = response.Content;
				LastUpdate = DateTime.Now;

				return true;
			} finally {
				UpdateSemaphore.Release();
			}
		}
	}
}
