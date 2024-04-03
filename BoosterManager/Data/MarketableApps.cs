using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;
using BoosterManager.Localization;

namespace BoosterManager {
	internal static class MarketableApps {
		internal static HashSet<uint> AppIDs = new();
		
		private static Uri Source = new("https://raw.githubusercontent.com/Citrinate/Steam-MarketableApps/main/data/marketable_apps.min.json");
		private static TimeSpan UpdateFrequency = TimeSpan.FromMinutes(30);
		private static TimeSpan SteamUpdateFrequency = TimeSpan.FromMinutes(5);

		private static DateTime? LastUpdate;
		private static DateTime? LastSteamUpdate;
		private static SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);

		internal static async Task<bool> Update() {
			ArgumentNullException.ThrowIfNull(ASF.WebBrowser);

			await UpdateSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				if (LastUpdate != null && (LastUpdate + UpdateFrequency) > DateTime.Now) {
					// Source data is still fresh, though it won't hurt to try to update from Steam
					await UpdateFromSteam().ConfigureAwait(false);

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

				// We're good to stop here, but let's try to replace the cached data that may be up to 1 hour old with fresh data from Steam
				await UpdateFromSteam().ConfigureAwait(false);

				return true;
			} finally {
				UpdateSemaphore.Release();
			}
		}

		private static async Task UpdateFromSteam() {
			if (LastSteamUpdate != null && (LastSteamUpdate + SteamUpdateFrequency) > DateTime.Now) {
				return;
			}

			if (AppIDs.Count == 0) {
				// Without known good data, we can't tell if the data recieve from Steam is good or not
				return;
			}

			Bot? bot = Bot.BotsReadOnly?.Values.FirstOrDefault(static bot => bot.IsConnectedAndLoggedOn);
			if (bot == null) {
				return;
			}

			MethodInfo? GetMarketableAppIDs = typeof(Bot).GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).FirstOrDefault(x => x.Name == "GetMarketableAppIDs");
			if (GetMarketableAppIDs == null) {
				ASF.ArchiLogger.LogGenericError(Strings.PluginError);

				return;
			}

			HashSet<uint>? newerAppIDs;
			try {
				var res = (Task<HashSet<uint>?>?) GetMarketableAppIDs.Invoke(bot, new object[]{});
				if (res == null) {
					ASF.ArchiLogger.LogNullError(res);

					return;
				}

				await res.ConfigureAwait(false);
				if (res.Result == null) {
					ASF.ArchiLogger.LogNullError(res.Result);

					return;
				}

				newerAppIDs = res.Result;
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);

				return;
			}

			LastSteamUpdate = DateTime.Now;

			if (AppIDs.Count - newerAppIDs.Count > 1000) {
				// Bad data from Steam, ignore it
				return;
			}

			AppIDs = newerAppIDs;
		}
	}
}
