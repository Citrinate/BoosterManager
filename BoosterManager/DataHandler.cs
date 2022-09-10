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
		internal static uint NumMarketHistoryPages = 1; // Number of market history pages to fetch
		internal static uint MarketHistoryDelay = 5; // Delay, in seconds, between each market history page fetch

		internal static async Task<string> SendAllData(Bot bot) {
			List<Task<string?>> tasks = new List<Task<string?>>();
			tasks.Add(SendBoosterData(bot));
			tasks.Add(SendMarketListings(bot));
			for (uint page = 0; page < NumMarketHistoryPages; page++) {
				uint delayInMilliseconds = page * MarketHistoryDelay * 1000;
				tasks.Add(SendMarketHistory(bot, page, delayInMilliseconds));
			}

			List<string> responses = (await Task.WhenAll(tasks).ConfigureAwait(false)).WhereNotNull().ToList<string>();
			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, "API endpoints not defined");
			}

			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		internal static async Task<string?> SendBoosterData(Bot bot, uint delayInMilliseconds = 0) {
			if (BoosterDataAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds);
			}

			(IDocument? boosterPage, Uri source) = await WebRequest.GetBoosterPage(bot).ConfigureAwait(false);

			if (boosterPage == null) {
				return "Failed to fetch Booster Data!";
			}

			IEnumerable<Steam.BoosterInfo>? boosterInfos = BoosterQueue.ParseBoosterPage(bot, boosterPage);
			
			if (boosterInfos == null) {
				return "Failed to parse Booster Data!";
			}

			(bool success, string? message) = await SendSteamData<IEnumerable<Steam.BoosterInfo>>(BoosterDataAPI, bot, boosterInfos, source).ConfigureAwait(false);

			if (message != null) {
				return message;
			}

			if (!success) {
				return "API failed to accept Booster Data!";
			}

			return "Successly sent Booster Data";
		}

		internal static async Task<string?> SendMarketListings(Bot bot, uint delayInMilliseconds = 0) {
			if (MarketListingsAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds);
			}

			(Steam.MarketListingsResponse? marketListings, Uri source) = await WebRequest.GetMarketListings(bot).ConfigureAwait(false);

			if (marketListings == null || !marketListings.Success) {
				return "Failed to fetch Market Listings!";
			}
			
			(bool success, string? message) = await SendSteamData<Steam.MarketListingsResponse>(MarketListingsAPI, bot, marketListings, source).ConfigureAwait(false);

			if (message != null) {
				return message;
			}

			if (!success) {
				return "API failed to accept Market Listings!";
			}

			return "Successfully sent Market Listings";
		}

		internal static async Task<string?> SendMarketHistory(Bot bot, uint page = 0, uint delayInMilliseconds = 0) {
			if (MarketHistoryAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds);
			}

			uint count = 500;
			uint start = page * count;
			(Steam.MarketHistoryResponse? marketHistory, Uri source) = await WebRequest.GetMarketHistory(bot, start, count).ConfigureAwait(false);

			if (marketHistory == null || !marketHistory.Success) {
				return String.Format("Failed to fetch Market History (Page {0})!", page + 1);
			}
			
			(bool success, string? message) = await SendSteamData<Steam.MarketHistoryResponse>(MarketHistoryAPI, bot, marketHistory, source).ConfigureAwait(false);

			if (message != null) {
				return message;
			}

			if (!success) {
				return String.Format("API failed to accept Market History (Page {0})!", page + 1);
			}

			return String.Format("Successfully sent Market History (Page {0})", page + 1);
		}

		private static async Task<(bool, string?)> SendSteamData<T>(Uri request, Bot bot, T steamData, Uri source) {
			SteamData<T> data = new SteamData<T>(bot, steamData, source);
			ObjectResponse<Steam.DataAPIResponse>? response = await bot.ArchiWebHandler.WebBrowser.UrlPostToJsonObject<Steam.DataAPIResponse, SteamData<T>>(request, data: data).ConfigureAwait(false);

			return (response?.Content?.Success ?? false, response?.Content?.Message);
		}
	}
}
