using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
		private static ConcurrentDictionary<string, DateTime> ForceStop = new();

		internal static async Task<string> SendAllData(Bot bot) {
			if (BoosterDataAPI == null && InventoryHistoryAPI == null && MarketListingsAPI == null && MarketHistoryAPI == null) {
				return Commands.FormatBotResponse(bot, "API endpoints not defined");
			}

			List<Task<string?>> tasks = new List<Task<string?>>();
			tasks.Add(SendBoosterData(bot));
			tasks.Add(SendMarketListings(bot));
			tasks.Add(SendMarketHistory(bot, tasks, DateTime.Now));
			tasks.Add(SendInventoryHistory(bot, tasks, DateTime.Now));

			while (tasks.Any(task => !task.IsCompleted)) {
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			List<string> responses = tasks.Select(task => task.Result).WhereNotNull().ToList<string>();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, "No messages to display");
			}

			responses.Add("");

			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static async Task<string> SendInventoryHistoryOnly(Bot bot, Bot respondingBot, ulong recipientSteamID, uint? numPages = 1, uint? startTime = null) {
			if (InventoryHistoryAPI == null) {
				return Commands.FormatBotResponse(bot, "Inventory History API endpoint not defined");
			}

			if (numPages == 0) {
				return Commands.FormatBotResponse(bot, "Finished sending no pages");
			}

			List<Task<string?>> tasks = new List<Task<string?>>();
			numPages = numPages ?? 1;
			tasks.Add(SendInventoryHistory(bot, tasks, DateTime.Now, startTime: startTime, pagesRemaining: numPages.Value - 1, retryOnRateLimit: true, respondingBot: respondingBot, recipientSteamID: recipientSteamID));

			while (tasks.Any(task => !task.IsCompleted)) {
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			List<string> responses = tasks.Select(task => task.Result).WhereNotNull().ToList<string>();

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

			List<Task<string?>> tasks = new List<Task<string?>>();
			numPages = numPages ?? 1;
			startPage = startPage ?? 0;
			tasks.Add(SendMarketHistory(bot, tasks, DateTime.Now, startPage.Value, pagesRemaining: numPages.Value - 1));

			while (tasks.Any(task => !task.IsCompleted)) {
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			List<string> responses = tasks.Select(task => task.Result).WhereNotNull().ToList<string>();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, "No messages to display");
			}

			responses.Add("");

			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static string StopSend(Bot bot) {
			ForceStop.AddOrUpdate(bot.BotName, DateTime.Now, (_, _) => DateTime.Now);

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

		private static async Task<string?> SendInventoryHistory(Bot bot, List<Task<string?>> tasks, DateTime tasksStartedTime, Steam.InventoryHistoryCursor? cursor = null, uint? startTime = null, uint pagesRemaining = 0, uint delayInMilliseconds = 0, bool retryOnRateLimit = false, bool showRateLimitMessage = true, Bot? respondingBot = null, ulong? recipientSteamID = null) {
			if (InventoryHistoryAPI == null) {
				return null;
			}

			uint pageTime = cursor?.Time ?? startTime ?? (uint) DateTimeOffset.Now.ToUnixTimeSeconds();

			if (delayInMilliseconds != 0) {
				for (int i = 0; i < delayInMilliseconds; i += 1000) {
					if (WasManuallyStopped(bot, tasksStartedTime)) {
						return String.Format("Manually stopped before fetching Inventory History for Time < {0} ({1:MMM d, yyyy @ h:mm:ss tt})", pageTime, GetDateTimeFromTimestamp(pageTime));
					}

					await Task.Delay(1000).ConfigureAwait(false);
				}
			}

			if (WasManuallyStopped(bot, tasksStartedTime)) {
				return String.Format("Manually stopped before fetching Inventory History for Time < {0} ({1:MMM d, yyyy @ h:mm:ss tt})", pageTime, GetDateTimeFromTimestamp(pageTime));
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return await SendInventoryHistory(bot, tasks, tasksStartedTime, cursor, startTime, pagesRemaining, 60 * 1000, retryOnRateLimit, showRateLimitMessage, respondingBot, recipientSteamID).ConfigureAwait(false);
			}

			pageTime = cursor?.Time ?? startTime ?? (uint) DateTimeOffset.Now.ToUnixTimeSeconds();
			Steam.InventoryHistoryResponse? inventoryHistory = null; 
			Uri? source = null;
			try {
				(inventoryHistory, source) = await WebRequest.GetInventoryHistory(bot, InventoryHistoryAppFilter, cursor, startTime).ConfigureAwait(false);
			} catch (HttpRequestException) {
				if (retryOnRateLimit) {
					// This API has a very reachable rate limit
					if (showRateLimitMessage && respondingBot != null && recipientSteamID != null) {
						string message = "Rate limit exceeded while attempting to fetch Inventory History. Will keep trying, but it could take up to 12 hours to continue.  If you'd like to stop, use the 'logstop' command.";					
						await respondingBot.SendMessage(recipientSteamID.Value, Commands.FormatBotResponse(bot, message)).ConfigureAwait(false);
					}

					// Try again in 15 minutes
					return await SendInventoryHistory(bot, tasks, tasksStartedTime, cursor, startTime, pagesRemaining, 15 * 60 * 1000, retryOnRateLimit, false, respondingBot, recipientSteamID).ConfigureAwait(false);
				}
			}

			if (inventoryHistory == null || !inventoryHistory.Success) {
				return String.Format("Failed to fetch Inventory History for Time < {0} ({1:MMM d, yyyy @ h:mm:ss tt})!", pageTime, GetDateTimeFromTimestamp(pageTime));
			}

			SteamDataResponse response = await SendSteamData<Steam.InventoryHistoryResponse>(InventoryHistoryAPI, bot, inventoryHistory, source!, pageTime, cursor).ConfigureAwait(false);

			if (response.GetNextPage && pagesRemaining == 0) {
				pagesRemaining = 1;
			}

			if (response.Success && pagesRemaining > 0) {
				if (response.NextCursor != null) {
					tasks.Add(SendInventoryHistory(bot, tasks, tasksStartedTime, response.NextCursor, null, pagesRemaining - 1, LogDataPageDelay * 1000, retryOnRateLimit, true, respondingBot, recipientSteamID));
				} else if (response.NextPage != null) {
					tasks.Add(SendInventoryHistory(bot, tasks, tasksStartedTime, null, response.NextPage, pagesRemaining - 1, LogDataPageDelay * 1000, retryOnRateLimit, true, respondingBot, recipientSteamID));
				} else if (inventoryHistory.Cursor != null) {
					tasks.Add(SendInventoryHistory(bot, tasks, tasksStartedTime, inventoryHistory.Cursor, null, pagesRemaining - 1, LogDataPageDelay * 1000, retryOnRateLimit, true, respondingBot, recipientSteamID));
				} else {
					// Inventory History has ended, possibly due to a bug described in ../Docs/InventoryHistory.md
					List<string> messages = new List<string>();
					if (response.ShowMessage) {
						if (response.Message != null) {
							messages.Add(response.Message);
						} else if (!response.Success) {
							messages.Add(String.Format("API failed to accept Inventory History for Time < {0} ({1:MMM d, yyyy @ h:mm:ss tt})!", pageTime, GetDateTimeFromTimestamp(pageTime)));
						} else {
							messages.Add(String.Format("Successfully sent Inventory History for Time < {0} ({1:MMM d, yyyy @ h:mm:ss tt})", pageTime, GetDateTimeFromTimestamp(pageTime)));
						}
					}
					messages.Add(String.Format("Inventory History ended at the page starting on {0:MMM d, yyyy @ h:mm:ss tt}", GetDateTimeFromTimestamp(pageTime)));
					messages.Add("Please verify that your history actually ends here, as there's a bug on Steam's end which can cause the history to end early.  Refer to the README for more information.");
					messages.Add(String.Format("({0})", source));

					return String.Join(Environment.NewLine, messages);
				}
			}

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return String.Format("API failed to accept Inventory History for Time < {0} ({1:MMM d, yyyy @ h:mm:ss tt})!", pageTime, GetDateTimeFromTimestamp(pageTime));
			}

			return String.Format("Successfully sent Inventory History for Time < {0} ({1:MMM d, yyyy @ h:mm:ss tt})", pageTime, GetDateTimeFromTimestamp(pageTime));
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

		private static async Task<string?> SendMarketHistory(Bot bot, List<Task<string?>> tasks, DateTime tasksStartedTime, uint page = 0, uint pagesRemaining = 0, uint delayInMilliseconds = 0) {
			if (MarketHistoryAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				for (int i = 0; i < delayInMilliseconds; i += 1000) {
					if (WasManuallyStopped(bot, tasksStartedTime)) {
						return String.Format("Manually stopped before fetching Market History (Page {0})", page + 1);
					}

					await Task.Delay(1000).ConfigureAwait(false);
				}
			}

			if (WasManuallyStopped(bot, tasksStartedTime)) {
				return String.Format("Manually stopped before fetching Market History (Page {0})", page + 1);
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return await SendMarketHistory(bot, tasks, tasksStartedTime, page, pagesRemaining, 60 * 1000).ConfigureAwait(false);
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

			if (response.Success && pagesRemaining > 0 && marketHistory.Events.Count > 0) {
				if (response.NextPage != null) {
					tasks.Add(SendMarketHistory(bot, tasks, tasksStartedTime, response.NextPage.Value - 1, pagesRemaining - 1, LogDataPageDelay * 1000));
				} else {
					tasks.Add(SendMarketHistory(bot, tasks, tasksStartedTime, page + 1, pagesRemaining - 1, LogDataPageDelay * 1000));
				}
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

		private static async Task<SteamDataResponse> SendSteamData<T>(Uri request, Bot bot, T steamData, Uri source, uint? page = null, Steam.InventoryHistoryCursor? cursor = null) {
			SteamData<T> data = new SteamData<T>(bot, steamData, source, page, cursor);
			ObjectResponse<SteamDataResponse>? response = await bot.ArchiWebHandler.WebBrowser.UrlPostToJsonObject<SteamDataResponse, SteamData<T>>(request, data: data).ConfigureAwait(false);

			if (response == null || response.Content == null) {
				return new SteamDataResponse();
			}

			return response.Content;
		}

		private static DateTime GetDateTimeFromTimestamp(uint timestamp) => DateTime.UnixEpoch.AddSeconds(timestamp).ToLocalTime();
		private static bool WasManuallyStopped(Bot bot, DateTime tasksStartedTime) => ForceStop.ContainsKey(bot.BotName) && ForceStop[bot.BotName] > tasksStartedTime;
	}
}
