using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngleSharp.Dom;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Web.Responses;

namespace BoosterManager {
	internal static class WebRequest {
		internal static async Task<(BoosterPageResponse?, Uri)> GetBoosterPage(Bot bot) {
			Uri request = new(ArchiWebHandler.SteamCommunityURL, "/tradingcards/boostercreator?l=english");
			HtmlDocumentResponse? boosterPage = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

			try {
				BoosterPageResponse boosterPageResponse = new BoosterPageResponse(bot, boosterPage?.Content);

				return (boosterPageResponse, request);
			} catch (Exception) {
				return (null, request);
			}
		}

		internal static async Task<Steam.BoostersResponse?> CreateBooster(Bot bot, uint appID, uint series, TradabilityPreference nTradabilityPreference) {
			if (appID == 0) {
				bot.ArchiLogger.LogNullError(null, nameof(appID));

				return null;
			}

			Uri request = new(ArchiWebHandler.SteamCommunityURL, "/tradingcards/ajaxcreatebooster");

			// Extra entry for sessionID
			Dictionary<string, string> data = new(4) {
				{ "appid", appID.ToString() },
				{ "series", series.ToString() },
				{ "tradability_preference", ((int) nTradabilityPreference).ToString() }
			};

			ObjectResponse<Steam.BoostersResponse>? createBoosterResponse = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<Steam.BoostersResponse>(request, data: data).ConfigureAwait(false);

			return createBoosterResponse?.Content;
		}

		internal static async Task<(Steam.MarketListingsResponse?, Uri)> GetMarketListings(Bot bot) {
			// The "listings" field returned here (the account's active sell orders) is paginated. The limits of the pagination can be stretched by setting the count to -1.  
			// A Github search suggests this will return as many as 1000 results instead of the standard 100. In my testing I got as high as 1804, seemingly without issue.
			// I don't intend to implement a way to fetch any/all of the pages however, as I personally don't use this field, and beacuse it can be reproduced using the market history.
			Uri request = new(ArchiWebHandler.SteamCommunityURL, "/market/mylistings?norender=1");
			ObjectResponse<Steam.MarketListingsResponse>? marketListingsResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<Steam.MarketListingsResponse>(request).ConfigureAwait(false);
			return (marketListingsResponse?.Content, request);
		}

		internal static async Task<(Steam.MarketHistoryResponse?, Uri)> GetMarketHistory(Bot bot, uint start = 0, uint count = 500) {
			count = Math.Min(count, 500);
			Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/market/myhistory?norender=1&start={0}&count={1}", start, count));
			ObjectResponse<Steam.MarketHistoryResponse>? marketHistoryResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<Steam.MarketHistoryResponse>(request).ConfigureAwait(false);
			return (marketHistoryResponse?.Content, request);
		}
	}
}
