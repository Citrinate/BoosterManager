using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;
using Newtonsoft.Json.Linq;
using Nito.Disposables.Internals;

namespace BoosterManager {
	internal sealed class DataHandler {
		internal static Uri? BoosterDataAPI = null;
		internal static Uri? MarketListingsAPI = null;
		internal static Uri? MarketHistoryAPI = null;
		internal static uint MarketHistoryDelay = 15; // Delay, in seconds, between each market history page fetch

		internal static async Task<string> SendAllData(Bot bot, uint? numMarketHistoryPages = 1, uint? marketHistoryStartPage = 0) {
			if (BoosterDataAPI == null && MarketListingsAPI == null && MarketHistoryAPI == null) {
				return Commands.FormatBotResponse(bot, "API endpoints not defined");
			}

			List<Task<string?>> tasks = new List<Task<string?>>();

			tasks.Add(SendBoosterData(bot));
			tasks.Add(SendMarketListings(bot));

			numMarketHistoryPages = numMarketHistoryPages ?? 1;
			marketHistoryStartPage = marketHistoryStartPage ?? 0;
			uint startPage = marketHistoryStartPage.Value;
			uint endPage = numMarketHistoryPages.Value + marketHistoryStartPage.Value;
			
			for (uint page = startPage; page < endPage; page++) {
				uint delayInMilliseconds = (page - startPage) * MarketHistoryDelay * 1000;
				tasks.Add(SendMarketHistory(bot, page, delayInMilliseconds));
			}

			List<string> responses = (await Task.WhenAll(tasks).ConfigureAwait(false)).WhereNotNull().ToList<string>();
			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, "No messages to display");
			}
			responses.Add("");

			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		private static async Task<string?> SendBoosterData(Bot bot, uint delayInMilliseconds = 0) {
			if (BoosterDataAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds).ConfigureAwait(false);
			}

			(BoosterPageResponse? boosterPage, Uri source) = await WebRequest.GetBoosterPage(bot).ConfigureAwait(false);

			if (boosterPage == null) {
				return "!Failed to fetch Booster Data!";
			}

			SteamDataResponse response = await SendSteamData<IEnumerable<Steam.BoosterInfo>>(BoosterDataAPI, bot, boosterPage.BoosterInfos, source).ConfigureAwait(false);

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return "!API failed to accept Booster Data!";
			}

			return "Successly sent Booster Data";
		}

		private static async Task<string?> SendMarketListings(Bot bot, uint delayInMilliseconds = 0) {
			if (MarketListingsAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds).ConfigureAwait(false);
			}

			// The "listings" field returned here is paginated, though I don't intend to add pagination features here.
			// Because I personally don't use this field, but also beacuse it can be reproduced using the market history.
			(Steam.MarketListingsResponse? marketListings, Uri source) = await WebRequest.GetMarketListings(bot).ConfigureAwait(false);

			if (marketListings == null || !marketListings.Success) {
				return "!Failed to fetch Market Listings!";
			}
			
			SteamDataResponse response = await SendSteamData<Steam.MarketListingsResponse>(MarketListingsAPI, bot, marketListings, source).ConfigureAwait(false);

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return "!API failed to accept Market Listings!";
			}

			return "Successfully sent Market Listings";
		}

		private static async Task<string?> SendMarketHistory(Bot bot, uint page = 0, uint delayInMilliseconds = 0) {
			if (MarketHistoryAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds).ConfigureAwait(false);
			}

			uint count = 500;
			uint start = page * count;
			(Steam.MarketHistoryResponse? marketHistory, Uri source) = await WebRequest.GetMarketHistory(bot, start, count).ConfigureAwait(false);

			if (marketHistory == null || !marketHistory.Success) {
				return String.Format("!Failed to fetch Market History (Page {0})!", page + 1);
			}
			
			SteamDataResponse response = await SendSteamData<Steam.MarketHistoryResponse>(MarketHistoryAPI, bot, marketHistory, source, page + 1).ConfigureAwait(false);

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return String.Format("!API failed to accept Market History (Page {0})!", page + 1);
			}

			return String.Format("Successfully sent Market History (Page {0})", page + 1);
		}

		private static async Task<SteamDataResponse> SendSteamData<T>(Uri request, Bot bot, T steamData, Uri source, uint? page = null) {
			SteamData<T> data = new SteamData<T>(bot, steamData, source, page);
			ObjectResponse<SteamDataResponse>? response = await bot.ArchiWebHandler.WebBrowser.UrlPostToJsonObject<SteamDataResponse, SteamData<T>>(request, data: data).ConfigureAwait(false);

			if (response == null || response.Content == null) {
				return new SteamDataResponse();
			}

			return response.Content;
		}

		internal static async Task<string> GetListings(Bot bot) {
			uint? listingsValue = await GetMarketListingsValue(bot).ConfigureAwait(false);

			if (listingsValue == null) {
				return "Failed to load Market Listings";
			}

			return Commands.FormatBotResponse(bot, String.Format("Listings: {0:#,#0.00} {1}", listingsValue / 100.0, bot.WalletCurrency.ToString()));
		}

		internal static async Task<string> GetValue(Bot bot, uint subtractFrom = 0) {
			uint? listingsValue = await GetMarketListingsValue(bot).ConfigureAwait(false);

			if (listingsValue == null) {
				return "Failed to load Market Listings";
			}

			if (subtractFrom != 0) {
				return Commands.FormatBotResponse(bot, String.Format("Remaining: {0:#,#0.00} {1}", subtractFrom - ((listingsValue + bot.WalletBalance) / 100.0), bot.WalletCurrency.ToString()));
			}

			return Commands.FormatBotResponse(bot, String.Format("Value: {0:#,#0.00} {1}", (listingsValue + bot.WalletBalance) / 100.0, bot.WalletCurrency.ToString()));
		}

		private static async Task<uint?> GetMarketListingsValue(Bot bot) {
			Dictionary<ulong, JObject>? listings = await GetFullMarketListings(bot).ConfigureAwait(false);

			if (listings == null) {
				return null;
			}

			uint listingsValue = 0;
			foreach (JObject listing in listings.Values) {
				JProperty? price = listing.Property("price");
				if (price == null) {
					bot.ArchiLogger.LogNullError(price);

					return null;
				}

				listingsValue += price.Value.ToObject<uint>();
			}

			return listingsValue;
		}

		private static async Task<Dictionary<ulong, JObject>?> GetFullMarketListings(Bot bot, uint delayInMilliseconds = 5000) {
			Dictionary<ulong, JObject>? listings = null;
			uint totalListings = 0;
			uint listingsCollected = 0;
			do {
				if (listingsCollected > 0 && delayInMilliseconds != 0) {
					await Task.Delay((int)delayInMilliseconds).ConfigureAwait(false);
				}

				// Normally, the maximum count here is 100, but we can use -1 to return many more than that
				(Steam.MarketListingsResponse? marketListings, _) = await WebRequest.GetMarketListings(bot, listingsCollected, -1).ConfigureAwait(false);

				if (marketListings == null || !marketListings.Success) {
					return null;
				}

				if (marketListings.Listings == null) {
					// This happens when our start is higher than the number of listings available
					// This should mean that, unexpectedly, we're finished
					break;
				}

				if (marketListings.Listings.GetType() != typeof(JArray)) {
					bot.ArchiLogger.LogGenericError(String.Format("Unexpected listings type: {0}", marketListings.Listings.GetType()));

					return null;
				}

				totalListings = marketListings.NumActiveListings;

				if (listings == null) {
					listings = new Dictionary<ulong, JObject>((int)totalListings);
				}

				foreach (JObject listing in marketListings.Listings) {
					JProperty? listingid = listing.Property("listingid");
					if (listingid == null) {
						bot.ArchiLogger.LogNullError(listingid);
						
						return null;
					}
					
					if (listings.TryAdd(listingid.Value.ToObject<ulong>(), listing)) {
						listingsCollected++;
					}
				}
			} while (listingsCollected < totalListings);

			return listings;
		}
	}
}
