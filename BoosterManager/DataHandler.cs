using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;
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

		internal static async Task<string?> SendBoosterData(Bot bot, uint delayInMilliseconds = 0) {
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

		internal static async Task<string?> SendMarketListings(Bot bot, uint delayInMilliseconds = 0) {
			if (MarketListingsAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds).ConfigureAwait(false);
			}

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

		internal static async Task<string?> SendMarketHistory(Bot bot, uint page = 0, uint delayInMilliseconds = 0) {
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
	}
}
