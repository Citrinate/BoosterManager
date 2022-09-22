using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
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

		internal static async Task<(Steam.MarketListingsResponse?, Uri)> GetMarketListings(Bot bot, uint start = 0, int count = 0) {
			count = Math.Min(count, 100);
			Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/market/mylistings?norender=1&start={0}&count={1}", start, count));
			ObjectResponse<Steam.MarketListingsResponse>? marketListingsResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<Steam.MarketListingsResponse>(request).ConfigureAwait(false);
			return (marketListingsResponse?.Content, request);
		}

		internal static async Task<(Steam.MarketHistoryResponse?, Uri)> GetMarketHistory(Bot bot, uint start = 0, uint count = 500) {
			count = Math.Min(count, 500);
			Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/market/myhistory?norender=1&start={0}&count={1}", start, count));
			ObjectResponse<Steam.MarketHistoryResponse>? marketHistoryResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<Steam.MarketHistoryResponse>(request).ConfigureAwait(false);
			return (marketHistoryResponse?.Content, request);
		}

		internal static async Task<Steam.ExchangeGooResponse?> UnpackGems(Bot bot, ulong assetID, uint amount) {
			Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/profiles/{0}/ajaxexchangegoo", bot.SteamID));

			// Extra entry for sessionID
			Dictionary<string, string> data = new(7) {
				{ "appid", Asset.SteamAppID.ToString() },
				{ "assetid", assetID.ToString() },
				{ "goo_denomination_in", "1000" },
				{ "goo_amount_in", amount.ToString() },
				{ "goo_denomination_out", "1" },
				{ "goo_amount_out_expected", (amount * 1000).ToString() }
			};

			ObjectResponse<Steam.ExchangeGooResponse>? unpackBoostersResponse = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<Steam.ExchangeGooResponse>(request, data: data).ConfigureAwait(false);

			return unpackBoostersResponse?.Content;
		}

		internal static async Task<bool> RemoveListing(Bot bot, ulong listingID) {
			Uri request = new(ArchiWebHandler.SteamCommunityURL, $"/market/removelisting/{listingID}");
			Uri referer = new(ArchiWebHandler.SteamCommunityURL, "/market/");
			Dictionary<string, string> headers = new(1) {
				{ "Cookie", bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieHeader(ArchiWebHandler.SteamCommunityURL) }
			};

			return await bot.ArchiWebHandler.UrlPostWithSession(request, referer: referer, headers: headers).ConfigureAwait(false);
		}
	}
}
