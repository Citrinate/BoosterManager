using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Web.Responses;

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
					// Data is still fresh
					return true;
				}

				// https://api.steampowered.com/ISteamApps/GetApplist/v2 can be used to get a list which includes all marketable apps and excludes all unmarketable apps
				// It's however not reliable and also not perfect.  At random times, tens of thousands of apps will be missing (some of which are marketable)
				// Can't account for these errors whithin this plugin (in a timely fashion), and so we use a cached version of ISteamApps/GetApplist which is known to be good

				ObjectResponse<HashSet<uint>>? response = await ASF.WebBrowser.UrlGetToJsonObject<HashSet<uint>>(Source).ConfigureAwait(false);
				if (response == null || response.Content == null) {
					ASF.ArchiLogger.LogGenericDebug("Failed to fetch marketable apps data");

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
