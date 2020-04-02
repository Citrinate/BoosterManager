using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using ArchiSteamFarm;

namespace BoosterCreator {
	internal static class WebRequest {
		internal static async Task<IDocument> GetBoosterPage(Bot bot) {
			const string request = "/tradingcards/boostercreator?l=english";

			return await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(SteamCommunityURL, request).ConfigureAwait(false);
		}

		internal static async Task<Steam.BoostersResponse> CreateBooster(Bot bot, uint appID, uint series, uint nTradabilityPreference) {
			if (appID == 0) {
				bot.ArchiLogger.LogNullError(nameof(appID));

				return null;
			}

			const string request = "/tradingcards/ajaxcreatebooster";

			// Extra entry for sessionID
			Dictionary<string, string> data = new Dictionary<string, string>(4) {
				{ "appid", appID.ToString() },
				{ "series", series.ToString() },
				{ "tradability_preference", nTradabilityPreference.ToString() }
			};

			return await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<Steam.BoostersResponse>(SteamCommunityURL, request, data).ConfigureAwait(false);
		}

		internal static string SteamCommunityURL => ArchiWebHandler.SteamCommunityURL;
	}
}
