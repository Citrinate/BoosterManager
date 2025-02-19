using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;
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
				return Commands.FormatBotResponse(bot, Strings.APIEndpointsUndefined);
			}

			List<Task<string?>> tasks = new List<Task<string?>>();
			tasks.Add(SendBoosterData(bot, DateTime.Now));
			tasks.Add(SendMarketListings(bot));
			tasks.Add(SendMarketHistory(bot, tasks, DateTime.Now));
			tasks.Add(SendInventoryHistory(bot, tasks, DateTime.Now));

			while (tasks.Any(task => !task.IsCompleted)) {
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			List<string> responses = tasks.Select(task => task.Result).WhereNotNull().ToList<string>();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, Strings.NoMessages);
			}

			if (responses.Count > 1) {
				responses.Add("");
			}
			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static async Task<string> SendBoosterDataOnly(Bot bot) {
			if (BoosterDataAPI == null) {
				return Commands.FormatBotResponse(bot, Strings.BoosterEndpointUndefined);
			}

			List<Task<string?>> tasks = new List<Task<string?>>();
			tasks.Add(SendBoosterData(bot, DateTime.Now, retryOnFailure: true));

			while (tasks.Any(task => !task.IsCompleted)) {
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			List<string> responses = tasks.Select(task => task.Result).WhereNotNull().ToList<string>();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, Strings.NoMessages);
			}

			if (responses.Count > 1) {
				responses.Add("");
			}
			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static async Task<string> SendInventoryHistoryOnly(Bot bot, StatusReporter rateLimitReporter, uint? numPages = 1, uint? startTime = null, uint? timeFrac = null, string? s = null) {
			if (InventoryHistoryAPI == null) {
				return Commands.FormatBotResponse(bot, Strings.InventoryHistoryEndpointUndefined);
			}

			if (numPages == 0) {
				return Commands.FormatBotResponse(bot, Strings.FinishedZeroPages);
			}

			List<Task<string?>> tasks = new List<Task<string?>>();
			numPages = numPages ?? 1;
			if (startTime != null && timeFrac != null && s != null) {
				Steam.InventoryHistoryCursor cursor = new Steam.InventoryHistoryCursor(startTime.Value, timeFrac.Value, s);
				tasks.Add(SendInventoryHistory(bot, tasks, DateTime.Now, cursor: cursor, pagesRemaining: numPages.Value - 1, retryOnRateLimit: true, rateLimitReporter: rateLimitReporter));
			} else {
				tasks.Add(SendInventoryHistory(bot, tasks, DateTime.Now, startTime: startTime, pagesRemaining: numPages.Value - 1, retryOnRateLimit: true, rateLimitReporter: rateLimitReporter));
			}

			while (tasks.Any(task => !task.IsCompleted)) {
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			List<string> responses = tasks.Select(task => task.Result).WhereNotNull().ToList<string>();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, Strings.NoMessages);
			}

			if (responses.Count > 1) {
				responses.Add("");
			}
			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static async Task<string> SendMarketListingsOnly(Bot bot) {
			if (MarketListingsAPI == null) {
				return Commands.FormatBotResponse(bot, Strings.MarketListingsEndpointUndefined);
			}

			List<Task<string?>> tasks = new List<Task<string?>>();
			tasks.Add(SendMarketListings(bot));

			while (tasks.Any(task => !task.IsCompleted)) {
				await Task.WhenAll(tasks).ConfigureAwait(false);
			}

			List<string> responses = tasks.Select(task => task.Result).WhereNotNull().ToList<string>();

			if (responses.Count == 0) {
				return Commands.FormatBotResponse(bot, Strings.NoMessages);
			}

			if (responses.Count > 1) {
				responses.Add("");
			}
			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static async Task<string> SendMarketHistoryOnly(Bot bot, uint? numPages = 1, uint? startPage = 0) {
			if (MarketHistoryAPI == null) {
				return Commands.FormatBotResponse(bot, Strings.MarketHistoryEndpointUndefined);
			}

			if (numPages == 0) {
				return Commands.FormatBotResponse(bot, Strings.FinishedZeroPages);
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
				return Commands.FormatBotResponse(bot, Strings.NoMessages);
			}

			if (responses.Count > 1) {
				responses.Add("");
			}
			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		public static string StopSend(Bot bot) {
			ForceStop[bot.BotName] = DateTime.Now;

			return Commands.FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.Success);
		}

		private static async Task<string?> SendBoosterData(Bot bot, DateTime tasksStartedTime, uint delayInMilliseconds = 0, bool retryOnFailure = false) {
			if (BoosterDataAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				for (int i = 0; i < delayInMilliseconds; i += 1000) {
					if (WasManuallyStopped(bot, tasksStartedTime)) {
						return String.Format(Strings.BoosterDataFetchStopped);
					}

					await Task.Delay(1000).ConfigureAwait(false);
				}
			}

			if (WasManuallyStopped(bot, tasksStartedTime)) {
				return String.Format(Strings.BoosterDataFetchStopped);
			}

			(BoosterPageResponse? boosterPage, Uri source) = await WebRequest.GetBoosterPage(bot).ConfigureAwait(false);

			if (boosterPage == null) {
				if (retryOnFailure) {
					return await SendBoosterData(bot, tasksStartedTime, LogDataPageDelay * 1000, retryOnFailure).ConfigureAwait(false);
				}

				return String.Format("{0} :steamthumbsdown:", Strings.BoosterDataFetchFailed);
			}

			SteamDataResponse response = await WebRequest.SendSteamData<IEnumerable<Steam.BoosterInfo>>(BoosterDataAPI, bot, boosterPage.BoosterInfos, source).ConfigureAwait(false);

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return String.Format("{0} :steamthumbsdown:", Strings.BoosterDataEndpointFailed);
			}

			return Strings.BoosterDataEndpointSuccess;
		}

		private static async Task<string?> SendInventoryHistory(Bot bot, List<Task<string?>> tasks, DateTime tasksStartedTime, Steam.InventoryHistoryCursor? cursor = null, uint? startTime = null, uint pagesRemaining = 0, uint delayInMilliseconds = 0, bool retryOnRateLimit = false, bool showRateLimitMessage = true, StatusReporter? rateLimitReporter = null) {
			if (InventoryHistoryAPI == null) {
				return null;
			}

			uint pageTime = cursor?.Time ?? startTime ?? (uint) DateTimeOffset.Now.ToUnixTimeSeconds();

			if (delayInMilliseconds != 0) {
				for (int i = 0; i < delayInMilliseconds; i += 1000) {
					if (WasManuallyStopped(bot, tasksStartedTime)) {
						return String.Format(Strings.InventoryHistoryFetchStopped, pageTime, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime)));
					}

					await Task.Delay(1000).ConfigureAwait(false);
				}
			}

			if (WasManuallyStopped(bot, tasksStartedTime)) {
				return String.Format(Strings.InventoryHistoryFetchStopped, pageTime, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime)));
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return await SendInventoryHistory(bot, tasks, tasksStartedTime, cursor, startTime, pagesRemaining, 60 * 1000, retryOnRateLimit, showRateLimitMessage, rateLimitReporter).ConfigureAwait(false);
			}

			pageTime = cursor?.Time ?? startTime ?? (uint) DateTimeOffset.Now.ToUnixTimeSeconds();
			Steam.InventoryHistoryResponse? inventoryHistory = null; 
			Uri? source = null;
			try {
				(inventoryHistory, source) = await WebRequest.GetInventoryHistory(bot, InventoryHistoryAppFilter, cursor, startTime).ConfigureAwait(false);
			} catch (InventoryHistoryException) {
				if (retryOnRateLimit) {
					// This API has a very reachable rate limit
					if (showRateLimitMessage && rateLimitReporter != null) {
						string message = Strings.InventoryHistoryRateLimitExceeded;
						rateLimitReporter.Report(bot, message);
					}

					// Try again in 15 minutes
					return await SendInventoryHistory(bot, tasks, tasksStartedTime, cursor, startTime, pagesRemaining, 15 * 60 * 1000, retryOnRateLimit, false, rateLimitReporter).ConfigureAwait(false);
				}
			}

			if (inventoryHistory == null || !inventoryHistory.Success) {
				if (!bot.IsConnectedAndLoggedOn) {
					return await SendInventoryHistory(bot, tasks, tasksStartedTime, cursor, startTime, pagesRemaining, 60 * 1000, retryOnRateLimit, showRateLimitMessage, rateLimitReporter).ConfigureAwait(false);
				}

				if (inventoryHistory?.Error != null) {
					return String.Format("{0}: {1} :steamthumbsdown:", String.Format(Strings.InventoryHistoryFetchFailed, pageTime, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime))), inventoryHistory.Error);
				}


				return String.Format("{0} :steamthumbsdown:", String.Format(Strings.InventoryHistoryFetchFailed, pageTime, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime))));
			}

			SteamDataResponse response = await WebRequest.SendSteamData<Steam.InventoryHistoryResponse>(InventoryHistoryAPI, bot, inventoryHistory, source!, pageTime, cursor).ConfigureAwait(false);

			if (response.GetNextPage && pagesRemaining == 0) {
				pagesRemaining = 1;
			}

			if (response.Success && pagesRemaining > 0) {
				if (response.NextCursor != null) {
					tasks.Add(SendInventoryHistory(bot, tasks, tasksStartedTime, response.NextCursor, null, pagesRemaining - 1, LogDataPageDelay * 1000, retryOnRateLimit, true, rateLimitReporter));
				} else if (response.NextPage != null) {
					tasks.Add(SendInventoryHistory(bot, tasks, tasksStartedTime, null, response.NextPage, pagesRemaining - 1, LogDataPageDelay * 1000, retryOnRateLimit, true, rateLimitReporter));
				} else if (inventoryHistory.Cursor != null) {
					tasks.Add(SendInventoryHistory(bot, tasks, tasksStartedTime, inventoryHistory.Cursor, null, pagesRemaining - 1, LogDataPageDelay * 1000, retryOnRateLimit, true, rateLimitReporter));
				} else {
					// Inventory History has ended, possibly due to a bug described in ../Docs/InventoryHistory.md
					List<string> messages = new List<string>();
					if (response.ShowMessage) {
						if (response.Message != null) {
							messages.Add(response.Message);
						} else if (!response.Success) {
							messages.Add(String.Format("{0} :steamthumbsdown:", String.Format(Strings.InventoryHistoryEndpointFailed, pageTime, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime)))));
						} else {
							messages.Add(String.Format(Strings.InventoryHistoryEndpointSuccess, pageTime, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime))));
						}
					}
					messages.Add(String.Format(Strings.InventoryHistoryEnded, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime))));
					messages.Add(Strings.InventoryHistorySteamError);
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
				return String.Format("{0} :steamthumbsdown:", String.Format(Strings.InventoryHistoryEndpointFailed, pageTime, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime))));
			}

			return String.Format(Strings.InventoryHistoryEndpointSuccess, pageTime, String.Format("{0:MMM d, yyyy}", GetDateTimeFromTimestamp(pageTime)), String.Format("{0:T}", GetDateTimeFromTimestamp(pageTime)));
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
			(Steam.MarketListingsResponse? marketListings, Uri source) = await WebRequest.GetMarketListings(bot, 0, -1).ConfigureAwait(false);

			if (marketListings == null || !marketListings.Success) {
				return String.Format("{0} :steamthumbsdown:", Strings.MarketListingsFetchFailed);
			}
			
			SteamDataResponse response = await WebRequest.SendSteamData<Steam.MarketListingsResponse>(MarketListingsAPI, bot, marketListings, source).ConfigureAwait(false);

			if (!response.ShowMessage) {
				return null;
			}

			if (response.Message != null) {
				return response.Message;
			}

			if (!response.Success) {
				return String.Format("{0} :steamthumbsdown:", Strings.MarketListingsEndpointFailed);
			}

			return Strings.MarketListingsEndpointSuccess;
		}

		private static async Task<string?> SendMarketHistory(Bot bot, List<Task<string?>> tasks, DateTime tasksStartedTime, uint page = 0, uint pagesRemaining = 0, uint delayInMilliseconds = 0) {
			if (MarketHistoryAPI == null) {
				return null;
			}

			if (delayInMilliseconds != 0) {
				for (int i = 0; i < delayInMilliseconds; i += 1000) {
					if (WasManuallyStopped(bot, tasksStartedTime)) {
						return String.Format(Strings.MarketHistoryFetchStopped, page + 1);
					}

					await Task.Delay(1000).ConfigureAwait(false);
				}
			}

			if (WasManuallyStopped(bot, tasksStartedTime)) {
				return String.Format(Strings.MarketHistoryFetchStopped, page + 1);
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return await SendMarketHistory(bot, tasks, tasksStartedTime, page, pagesRemaining, 60 * 1000).ConfigureAwait(false);
			}

			uint count = 500;
			uint start = page * count;
			(Steam.MarketHistoryResponse? marketHistory, Uri source) = await WebRequest.GetMarketHistory(bot, start, count).ConfigureAwait(false);

			if (marketHistory == null || !marketHistory.Success) {
				return String.Format("{0} :steamthumbsdown:", String.Format(Strings.MarketHistoryFetchFailed, page + 1));
			}

			SteamDataResponse response = await WebRequest.SendSteamData<Steam.MarketHistoryResponse>(MarketHistoryAPI, bot, marketHistory, source, page + 1).ConfigureAwait(false);

			if (response.GetNextPage && pagesRemaining == 0) {
				pagesRemaining = 1;
			}

			if (response.Success && pagesRemaining > 0 && marketHistory.Events?.Count > 0) {
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
				return String.Format("{0} :steamthumbsdown:", String.Format(Strings.MarketHistoryEndpointFailed, page + 1));
			}

			return String.Format(Strings.MarketHistoryEndpointSuccess, page + 1);
		}

		private static DateTime GetDateTimeFromTimestamp(uint timestamp) => DateTime.UnixEpoch.AddSeconds(timestamp).ToLocalTime();
		private static bool WasManuallyStopped(Bot bot, DateTime tasksStartedTime) => ForceStop.ContainsKey(bot.BotName) && ForceStop[bot.BotName] > tasksStartedTime;
	}
}
