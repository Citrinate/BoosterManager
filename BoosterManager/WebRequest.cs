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

		internal static async Task<(Steam.InventoryHistoryResponse?, Uri)> GetInventoryHistory(Bot bot, List<uint>? appIDs = null, Steam.InventoryHistoryCursor? cursor = null, uint? startTime = null) {
			// This API has a rather restrictive rate limit of 1200 requests per 12 hours, per IP address

			// Below is a description of a very annoying cursor bug.  Dates are used for simplicity, in reality we're working with unix timestamps.

			/**
			There's no way to fetch specific pages, instead we need to specify a time, and we'll get results older than that time.
			The API returns a maximum of 50 items.  If more items exist, it will return a cursor object with the time of the very 
			next item. Sometimes however, the cursor object returned by the API will be missing, even if more items exist.  Repeated 
			calls will not fix this error.

			For example, a chain of requests starting at 5/2/21 and ending on 4/30/21 with a missing cursor object at the end might 
			look like this:

			5/2/21 > 5/1/21 > 4/30/21 > nothing

			When this happens, we can sort of "remind" Steam that results older than 4/30/21 exist by using the start_time parameter 
			with a date older than 4/30/21.  If we make a request with start_time=4/29/21, we can then go back to the 4/30/21 page and 
			the previously broken chain will be restored. Once the chain is restored it will continue on normally for a while.

			5/2/21 > 5/1/21 > 4/30/21 > 4/29/21 > 4/28/21 > ...

			Special care needs to be taken when selecting a value for start_time.  If we're stopped at 4/30/21 and then make a request 
			with start_time=1/10/20 instead of start_time=4/29/21, the previous chain of requests will still be restored, but it will 
			have a gap in it.

			5/2/21 > 5/1/21 > 4/30/21 > 1/10/20 > 1/9/20 > ...

			The gap can be filled in by using start_time to request results between 4/30/21 and 1/1/20.  For example with start_time=4/29/21

			5/2/21 > 5/1/21 > 4/30/21 > 4/29/21 > 4/28/21 > ... > 1/10/20 > 1/9/20 > ...

			It's possible from this point that the chain could break again between 4/28/21 and 1/10/20.  Instead of pointing to nothing, 
			it will point to 1/10/20

			5/2/21 > 5/1/21 > 4/30/21 > 4/29/21 > 4/28/21 > 4/27/21 > 4/26/21 > 1/10/20 > 1/9/20 > ...

			There's no real way to detect and fix these gaps.  It's best to avoid creating them in the first place.

			This issue was first discovered when trying to fetch history sequentially on a single account starting from 9/30/22 and using
			an app filter of [753, 730].  The issue arose after about 3,380 pages of history, when it reached 4/30/21.
			*/

			List<string> parameters = new List<string>();
			parameters.Add("ajax=1");
			if (cursor != null) {
				parameters.Add($"cursor[time]={cursor.Time}");
				parameters.Add($"cursor[time_frac]={cursor.TimeFrac}");
				parameters.Add($"cursor[s]={cursor.S}");
			} else if (startTime != null) {
				parameters.Add($"start_time={startTime}");
			}
			if (appIDs != null) {
				foreach (uint appID in appIDs) {
					parameters.Add($"app[]={appID}");
				}
			}
			Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/profiles/{0}/inventoryhistory/?{1}", bot.SteamID, String.Join("&", parameters)));
			ObjectResponse<Steam.InventoryHistoryResponse>? inventoryHistoryResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<Steam.InventoryHistoryResponse>(request).ConfigureAwait(false);
			return (inventoryHistoryResponse?.Content, request);
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

			return await bot.ArchiWebHandler.UrlPostWithSession(request, referer: referer).ConfigureAwait(false);
		}
	}
}
