using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Web.Responses;
using Nito.Disposables.Internals;

namespace BoosterManager {
	internal sealed class DataHandler {
		internal static Uri? BoosterDataAPI = null;
		internal static Uri? InventoryHistoryAPI = null;
		internal static Uri? MarketListingsAPI = null;
		internal static Uri? MarketHistoryAPI = null;
		internal static uint LogDataPageDelay = 15; // Delay, in seconds, between each page fetch
		internal static List<uint>? InventoryHistoryAppFilter = null;
		private static ConcurrentDictionary<string, List<Task<string?>>> Tasks = new();
		private static ConcurrentDictionary<string, bool> ForceStop = new();

		internal static async Task<string> SendAllData(Bot bot) {
			if (BoosterDataAPI == null && InventoryHistoryAPI == null && MarketListingsAPI == null && MarketHistoryAPI == null) {
				return Commands.FormatBotResponse(bot, "API endpoints not defined");
			}

			if (!Tasks.ContainsKey(bot.BotName)) {
				Tasks.TryAdd(bot.BotName, new List<Task<string?>>());
			}

			if (!ForceStop.ContainsKey(bot.BotName)) {
				ForceStop.TryAdd(bot.BotName, false);
			}

			ForceStop[bot.BotName] = false;

			if (Tasks[bot.BotName].Count != 0) {
				return Commands.FormatBotResponse(bot, "Bot is already sending data");
			}

			Tasks[bot.BotName].Add(SendBoosterData(bot));
			Tasks[bot.BotName].Add(SendInventoryHistory(bot));
			Tasks[bot.BotName].Add(SendMarketListings(bot));
			Tasks[bot.BotName].Add(SendMarketHistory(bot));

			while (Tasks[bot.BotName].Any(task => !task.IsCompleted)) {
				await Task.WhenAll(Tasks[bot.BotName]).ConfigureAwait(false);
			}

			List<string> responses = Tasks[bot.BotName].Select(task => task.Result).WhereNotNull().ToList<string>();
			Tasks[bot.BotName].Clear();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, "No messages to display");
			}

			responses.Add("");

			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static async Task<string> SendInventoryHistoryOnly(Bot bot, uint? numPages = 1, uint? startTime = null) {
			if (InventoryHistoryAPI == null) {
				return Commands.FormatBotResponse(bot, "Inventory History API endpoint not defined");
			}

			if (numPages == 0) {
				return Commands.FormatBotResponse(bot, "Finished sending no pages");
			}

			if (!Tasks.ContainsKey(bot.BotName)) {
				Tasks.TryAdd(bot.BotName, new List<Task<string?>>());
			}

			if (!ForceStop.ContainsKey(bot.BotName)) {
				ForceStop.TryAdd(bot.BotName, false);
			}

			ForceStop[bot.BotName] = false;

			if (Tasks[bot.BotName].Count != 0) {
				return Commands.FormatBotResponse(bot, "Bot is already sending data");
			}

			numPages = numPages ?? 1;

			Tasks[bot.BotName].Add(SendInventoryHistory(bot, startTime: startTime, pagesRemaining: numPages.Value - 1));

			while (Tasks[bot.BotName].Any(task => !task.IsCompleted)) {
				await Task.WhenAll(Tasks[bot.BotName]).ConfigureAwait(false);
			}

			List<string> responses = Tasks[bot.BotName].Select(task => task.Result).WhereNotNull().ToList<string>();
			Tasks[bot.BotName].Clear();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, "No messages to display");
			}

			responses.Add("");

			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static async Task<string> SendMarketHistoryOnly(Bot bot, uint? numPages = 1, uint? startPage = 0) {
			if (MarketHistoryAPI == null) {
				return Commands.FormatBotResponse(bot, "Market History API endpoint not defined");
			}

			if (numPages == 0) {
				return Commands.FormatBotResponse(bot, "Finished sending no pages");
			}

			if (!Tasks.ContainsKey(bot.BotName)) {
				Tasks.TryAdd(bot.BotName, new List<Task<string?>>());
			}

			if (!ForceStop.ContainsKey(bot.BotName)) {
				ForceStop.TryAdd(bot.BotName, false);
			}

			ForceStop[bot.BotName] = false;
			
			if (Tasks[bot.BotName].Count != 0) {
				return Commands.FormatBotResponse(bot, "Bot is already sending data");
			}

			numPages = numPages ?? 1;
			startPage = startPage ?? 0;

			Tasks[bot.BotName].Add(SendMarketHistory(bot, startPage.Value, pagesRemaining: numPages.Value - 1));

			while (Tasks[bot.BotName].Any(task => !task.IsCompleted)) {
				await Task.WhenAll(Tasks[bot.BotName]).ConfigureAwait(false);
			}

			List<string> responses = Tasks[bot.BotName].Select(task => task.Result).WhereNotNull().ToList<string>();
			Tasks[bot.BotName].Clear();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, "No messages to display");
			}

			responses.Add("");

			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static string StopSend(Bot bot) {
			if (ForceStop.ContainsKey(bot.BotName)) {
				ForceStop[bot.BotName] = true;
			}

			return Commands.FormatBotResponse(bot, Strings.Success);
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
				return "Failed to fetch Booster Data!";
			}

			SteamDataResponse response = await SendSteamData<IEnumerable<Steam.BoosterInfo>>(BoosterDataAPI, bot, boosterPage.BoosterInfos, source).ConfigureAwait(false);

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return "API failed to accept Booster Data!";
			}

			return "Successly sent Booster Data";
		}

		private static async Task<string?> SendInventoryHistory(Bot bot, Steam.InventoryHistoryCursor? cursor = null, uint? startTime = null, uint pagesRemaining = 0, uint delayInMilliseconds = 0) {
			if (InventoryHistoryAPI == null) {
				return null;
			}

			if (ForceStop.ContainsKey(bot.BotName) && ForceStop[bot.BotName]) {
				if (cursor != null || startTime != null) {
					return String.Format("Manually stopped before fetching Inventory History for Time < {0}", cursor?.Time ?? startTime);
				} else {
					return "Manually stopped before fetching Inventory History";
				}
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds).ConfigureAwait(false);
			}

			(Steam.InventoryHistoryResponse? inventoryHistory, Uri source) = await WebRequest.GetInventoryHistory(bot, InventoryHistoryAppFilter, cursor, startTime).ConfigureAwait(false);

			if (inventoryHistory == null || !inventoryHistory.Success) {
				if (cursor != null || startTime != null) {
					return String.Format("Failed to fetch Inventory History for Time < {0}!", cursor?.Time ?? startTime);
				} else {
					return "Failed to fetch Inventory History!";
				}
			}

			SteamDataResponse response = await SendSteamData<Steam.InventoryHistoryResponse>(InventoryHistoryAPI, bot, inventoryHistory, source, cursor?.Time ?? startTime).ConfigureAwait(false);

			if (response.GetNextPage && pagesRemaining == 0) {
				pagesRemaining = 1;
			}

			if (pagesRemaining > 0 && inventoryHistory.Cursor != null) {
				Tasks[bot.BotName].Add(SendInventoryHistory(bot, cursor: inventoryHistory.Cursor, delayInMilliseconds: LogDataPageDelay * 1000, pagesRemaining: pagesRemaining - 1));
			}

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				if (cursor != null || startTime != null) {
					return String.Format("API failed to accept Inventory History for Time < {0}!", cursor?.Time ?? startTime);
				} else {
					return "API failed to accept Inventory History!";
				}
			}

			return String.Format("Successfully sent Inventory History", cursor?.Time);
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
				return "Failed to fetch Market Listings!";
			}
			
			SteamDataResponse response = await SendSteamData<Steam.MarketListingsResponse>(MarketListingsAPI, bot, marketListings, source).ConfigureAwait(false);

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return "API failed to accept Market Listings!";
			}

			return "Successfully sent Market Listings";
		}

		private static async Task<string?> SendMarketHistory(Bot bot, uint page = 0, uint pagesRemaining = 0, uint delayInMilliseconds = 0) {
			if (MarketHistoryAPI == null) {
				return null;
			}

			if (ForceStop.ContainsKey(bot.BotName) && ForceStop[bot.BotName]) {
				return String.Format("Manually stopped before fetching Market History (Page {0})", page + 1);
			}

			if (delayInMilliseconds != 0) {
				await Task.Delay((int)delayInMilliseconds).ConfigureAwait(false);
			}

			uint count = 500;
			uint start = page * count;
			(Steam.MarketHistoryResponse? marketHistory, Uri source) = await WebRequest.GetMarketHistory(bot, start, count).ConfigureAwait(false);

			if (marketHistory == null || !marketHistory.Success) {
				return String.Format("Failed to fetch Market History (Page {0})!", page + 1);
			}

			SteamDataResponse response = await SendSteamData<Steam.MarketHistoryResponse>(MarketHistoryAPI, bot, marketHistory, source, page + 1).ConfigureAwait(false);

			if (response.GetNextPage && pagesRemaining == 0) {
				pagesRemaining = 1;
			}

			if (pagesRemaining > 0) {
				Tasks[bot.BotName].Add(SendMarketHistory(bot, page + 1, pagesRemaining - 1, LogDataPageDelay * 1000));
			}

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return String.Format("API failed to accept Market History (Page {0})!", page + 1);
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
