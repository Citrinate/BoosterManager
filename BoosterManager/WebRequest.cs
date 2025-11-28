using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Web;
using ArchiSteamFarm.Web.Responses;

namespace BoosterManager {
	internal static class WebRequest {
		private static SemaphoreSlim SendSteamDataSemaphore = new SemaphoreSlim(4, 4);
		private static SemaphoreSlim MarketRequestSemaphore = new SemaphoreSlim(1, 1);
		private const int MarketRequestDelaySeconds = 3;
		private const double MarketRemovalRequestDelaySeconds = 0.25;

		internal static async Task<(BoosterPageResponse?, Uri)> GetBoosterPage(Bot bot) {
			Uri request = new(ArchiWebHandler.SteamCommunityURL, "/tradingcards/boostercreator?l=english");
			HtmlDocumentResponse? boosterPage = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

			try {
				BoosterPageResponse boosterPageResponse = new BoosterPageResponse(bot, boosterPage?.Content);

				return (boosterPageResponse, request);
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);
				
				return (null, request);
			}
		}

		internal static async Task<Steam.BoostersResponse?> CreateBooster(Bot bot, uint appID, uint series, Steam.TradabilityPreference nTradabilityPreference) {
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
			ObjectResponse<Steam.InventoryHistoryResponse>? inventoryHistoryResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<Steam.InventoryHistoryResponse>(request, requestOptions: WebBrowser.ERequestOptions.ReturnClientErrors | WebBrowser.ERequestOptions.AllowInvalidBodyOnErrors).ConfigureAwait(false);
			
			if (inventoryHistoryResponse?.StatusCode == HttpStatusCode.TooManyRequests) {
				throw new InventoryHistoryException();
			}

			return (inventoryHistoryResponse?.Content, request);
		}

		internal static async Task<Steam.ExchangeGooResponse?> ExchangeGoo(Bot bot, ulong assetID, uint amount, bool pack = true) {
			Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/profiles/{0}/ajaxexchangegoo", bot.SteamID));

			uint gooDenominationIn;
			uint gooDenominationOut;
			uint gooAmountOutExpected;
			if (pack) {
				gooDenominationIn = 1000;
				gooDenominationOut = 1;
				gooAmountOutExpected = amount * 1000;
			} else {
				if (amount < 1000) {
					return null;
				}
				
				amount -= amount % 1000;
				gooDenominationIn = 1;
				gooDenominationOut = 1000;
				gooAmountOutExpected = amount / 1000;
			}

			// Extra entry for sessionID
			Dictionary<string, string> data = new(7) {
				{ "appid", Asset.SteamAppID.ToString() },
				{ "assetid", assetID.ToString() },
				{ "goo_denomination_in", gooDenominationIn.ToString() },
				{ "goo_amount_in", amount.ToString() },
				{ "goo_denomination_out", gooDenominationOut.ToString() },
				{ "goo_amount_out_expected", gooAmountOutExpected.ToString() }
			};

			ObjectResponse<Steam.ExchangeGooResponse>? exchangeGooResponse = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<Steam.ExchangeGooResponse>(request, data: data).ConfigureAwait(false);

			return exchangeGooResponse?.Content;
		}

		internal static async Task<bool> RemoveListing(Bot bot, ulong listingID) {
			await MarketRequestSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				Uri request = new(ArchiWebHandler.SteamCommunityURL, $"/market/removelisting/{listingID}");
				Uri referer = new(ArchiWebHandler.SteamCommunityURL, "/market/");

				return await bot.ArchiWebHandler.UrlPostWithSession(request, referer: referer, maxTries: 1).ConfigureAwait(false);
			} finally {
				Utilities.InBackground(
					async() => {
						await Task.Delay(TimeSpan.FromSeconds(MarketRemovalRequestDelaySeconds)).ConfigureAwait(false);
						MarketRequestSemaphore.Release();
					}
				);
			}
		}

		internal static async Task<SteamDataResponse> SendSteamData<T>(Uri request, Bot bot, T steamData, Uri source, uint? page = null, Steam.InventoryHistoryCursor? cursor = null) {
			await SendSteamDataSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				SteamData<T> data = new SteamData<T>(bot, steamData, source, page, cursor);
				ObjectResponse<SteamDataResponse>? response = await bot.ArchiWebHandler.WebBrowser.UrlPostToJsonObject<SteamDataResponse, SteamData<T>>(request, data: data, maxTries: 1).ConfigureAwait(false);

				if (response == null || response.Content == null) {
					return new SteamDataResponse();
				}

				return response.Content;
			} finally {
				SendSteamDataSemaphore.Release();
			}
		}

		internal static async Task<JsonDocument?> GetBadgeInfo(Bot bot, uint appID, uint border = 0) {
			Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/profiles/{0}/ajaxgetbadgeinfo/{1}?border={2}", bot.SteamID, appID, border));
			ObjectResponse<JsonDocument>? badgeInfoResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<JsonDocument>(request).ConfigureAwait(false);
			return badgeInfoResponse?.Content;
		}

		internal static async Task<JsonDocument?> GetPriceHistory(Bot bot, uint appID, string hashName) {
			await MarketRequestSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/market/pricehistory/?appid={0}&market_hash_name={1}", appID, Uri.EscapeDataString(hashName)));
				ObjectResponse<JsonDocument>? priceHistoryResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<JsonDocument>(request).ConfigureAwait(false);
				return priceHistoryResponse?.Content;
			} finally {
				Utilities.InBackground(
					async() => {
						await Task.Delay(TimeSpan.FromSeconds(MarketRequestDelaySeconds)).ConfigureAwait(false);
						MarketRequestSemaphore.Release();
					}
				);
			}
		}

		internal static async Task<MarketListingPageResponse?> GetMarketListing(Bot bot, uint appID, string hashName) {
			await MarketRequestSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/market/listings/{0}/{1}", appID, Uri.EscapeDataString(hashName)));
				HtmlDocumentResponse? marketListing = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(request).ConfigureAwait(false);

				try {
					MarketListingPageResponse marketListingPage = new MarketListingPageResponse(marketListing?.Content);

					return marketListingPage;
				} catch (Exception e) {
					ASF.ArchiLogger.LogGenericException(e);
					ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, request.AbsoluteUri));
					
					return null;
				}
			} finally {
				Utilities.InBackground(
					async() => {
						await Task.Delay(TimeSpan.FromSeconds(MarketRequestDelaySeconds)).ConfigureAwait(false);
						MarketRequestSemaphore.Release();
					}
				);
			}
		}

		internal static async Task<Steam.ItemOrdersHistogramResponse?> GetMarketPriceHistogram(Bot bot, uint nameID) {
			if (bot.WalletCurrency == SteamKit2.ECurrencyCode.Invalid) {
				return null;
			}

			await MarketRequestSemaphore.WaitAsync().ConfigureAwait(false);
			try {
				Uri request = new(ArchiWebHandler.SteamCommunityURL, String.Format("/market/itemordershistogram?language=english&currency={0}&item_nameid={1}", (int) bot.WalletCurrency, nameID));
				ObjectResponse<Steam.ItemOrdersHistogramResponse>? priceHistogramResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<Steam.ItemOrdersHistogramResponse>(request).ConfigureAwait(false);
				return priceHistogramResponse?.Content;
			} finally {
				Utilities.InBackground(
					async() => {
						await Task.Delay(TimeSpan.FromSeconds(MarketRequestDelaySeconds)).ConfigureAwait(false);
						MarketRequestSemaphore.Release();
					}
				);
			}
		}
	}
}
