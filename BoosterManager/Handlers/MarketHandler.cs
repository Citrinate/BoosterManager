using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using BoosterManager.Localization;

namespace BoosterManager {
	internal static class MarketHandler {
		private static ConcurrentDictionary<Bot, (Timer, StatusReporter?)> MarketRepeatTimers = new();
		private static Timer MarketAlertTimer = new(async e => await CheckMarketAlerts().ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite);
		private static TimeSpan MarketAlertUpdateFrequence = TimeSpan.FromMinutes(15);

		internal static async Task<string> GetListings(Bot bot) {
			uint? listingsValue = await GetMarketListingsValue(bot).ConfigureAwait(false);

			if (listingsValue == null) {
				return Strings.MarketListingsFetchFailed;
			}

			return Commands.FormatBotResponse(bot, String.Format(Strings.ListingsValue, String.Format("{0:#,#0.00}", listingsValue / 100.0), bot.WalletCurrency.ToString()));
		}

		internal static async Task<string> GetValue(Bot bot, uint subtractFrom = 0) {
			uint? listingsValue = await GetMarketListingsValue(bot).ConfigureAwait(false);

			if (listingsValue == null) {
				return Strings.MarketListingsFetchFailed;
			}

			var value = (listingsValue + bot.WalletBalance) / 100.0;
			var valueWithDelayed = (listingsValue + bot.WalletBalance + bot.WalletBalanceDelayed) / 100.0;

			if (subtractFrom != 0) {
				if (bot.WalletBalanceDelayed > 0) {
					return Commands.FormatBotResponse(bot, String.Format(Strings.AccountValueRemainingWithDelayed, String.Format("{0:#,#0.00}", subtractFrom - value), String.Format("{0:#,#0.00}", subtractFrom - valueWithDelayed), bot.WalletCurrency.ToString()));
				} else {
					return Commands.FormatBotResponse(bot, String.Format(Strings.AccountValueRemaining, String.Format("{0:#,#0.00}", subtractFrom - value), bot.WalletCurrency.ToString()));
				}
			}

			if (bot.WalletBalanceDelayed > 0) {
				return Commands.FormatBotResponse(bot, String.Format(Strings.AccountValueWithDelayed, String.Format("{0:#,#0.00}", value), String.Format("{0:#,#0.00}", valueWithDelayed), bot.WalletCurrency.ToString()));
			} else {
				return Commands.FormatBotResponse(bot, String.Format(Strings.AccountValue, String.Format("{0:#,#0.00}", value), bot.WalletCurrency.ToString()));
			}
		}

		private static async Task<uint?> GetMarketListingsValue(Bot bot) {
			Dictionary<ulong, JsonObject>? listings = await GetFullMarketListings(bot).ConfigureAwait(false);

			if (listings == null) {
				return null;
			}

			uint listingsValue = 0;
			foreach (JsonObject listing in listings.Values) {
				uint? price = listing["price"]?.ToString().ToJsonObject<uint>();
				if (price == null) {
					bot.ArchiLogger.LogNullError(price);

					return null;
				}

				listingsValue += price.Value;
			}

			return listingsValue;
		}

		internal static async Task<string> FindListings(Bot bot, List<ItemIdentifier> itemIdentifiers) {
			Dictionary<string, List<ulong>>? filteredListings = await GetListingIDsFromIdentifiers(bot, itemIdentifiers).ConfigureAwait(false);

			if (filteredListings == null) {
				return Commands.FormatBotResponse(bot, Strings.MarketListingsFetchFailed);
			}

			if (filteredListings.Count == 0) {
				return Commands.FormatBotResponse(bot, Strings.ListingsNotFound);
			}

			List<string> responses = new List<string>();
			foreach ((string itemName, List<ulong> listingIDs) in filteredListings) {
				responses.Add(String.Format(Strings.ListingsFound, listingIDs.Count, itemName, String.Join(", ", listingIDs)));
			}

			return Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, responses));
		}

		internal static async Task<string> RemoveListings(Bot bot, List<ulong> listingIDs) {
			List<ulong> failedListingIDs = new List<ulong>();
			foreach (ulong listingID in listingIDs) {
				if (!await WebRequest.RemoveListing(bot, listingID).ConfigureAwait(false)) {
					failedListingIDs.Add(listingID);
				}
			}
			
			if (failedListingIDs.Count != 0) {
				return Commands.FormatBotResponse(bot, String.Format(Strings.ListingsRemovedFailed, listingIDs.Count - failedListingIDs.Count, listingIDs.Count, String.Join(", ", failedListingIDs)));
			}

			return Commands.FormatBotResponse(bot, String.Format(Strings.ListingsRemovedSuccess, listingIDs.Count));
		}
		
		internal static async Task<string> RemovePendingListings(Bot bot) {
			(Steam.MarketListingsResponse? marketListings, _) = await WebRequest.GetMarketListings(bot).ConfigureAwait(false);

			if (marketListings == null || marketListings.ListingsToConfirm == null || !marketListings.Success) {
				return Strings.MarketListingsFetchFailed;
			}

			if (marketListings.ListingsToConfirm.Count == 0) {
				return Strings.PendingListingsNotFound;
			}

			HashSet<ulong> pendingListingIDs = new();

			foreach (JsonNode? listing in marketListings.ListingsToConfirm) {
				if (listing == null) {
					bot.ArchiLogger.LogNullError(listing);
						
					return Strings.MarketListingsFetchFailed;
				}

				ulong? listingid = listing["listingid"]?.ToString().ToJsonObject<ulong>();
				if (listingid == null) {
					bot.ArchiLogger.LogNullError(listingid);
						
					return Strings.MarketListingsFetchFailed;
				}
					
				pendingListingIDs.Add(listingid.Value);
			}

			int failedToRemove = 0;

			foreach (ulong listingID in pendingListingIDs) {
				if (!await WebRequest.RemoveListing(bot, listingID).ConfigureAwait(false)) {
					failedToRemove++;
				}
			}

			if (failedToRemove != 0) {
				return String.Format(Strings.PendingListingsRemovedFailed, pendingListingIDs.Count - failedToRemove, failedToRemove);
			}

			return String.Format(Strings.PendingListingsRemovedSuccess, pendingListingIDs.Count);
		}

		internal static async Task<string> FindAndRemoveListings(Bot bot, List<ItemIdentifier> itemIdentifiers) {
			Dictionary<string, List<ulong>>? filteredListings = await GetListingIDsFromIdentifiers(bot, itemIdentifiers).ConfigureAwait(false);

			if (filteredListings == null) {
				return Commands.FormatBotResponse(bot, Strings.MarketListingsFetchFailed);
			}

			if (filteredListings.Count == 0) {
				return Commands.FormatBotResponse(bot, Strings.ListingsNotFound);
			}

			List<ulong> listingIDs = filteredListings.Values.SelectMany(x => x).Distinct().ToList();

			return await RemoveListings(bot, listingIDs).ConfigureAwait(false);
		}

		private static async Task<Dictionary<ulong, JsonObject>?> GetFullMarketListings(Bot bot, uint delayInMilliseconds = 5000) {
			Dictionary<ulong, JsonObject>? listings = null;
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

				totalListings = marketListings.NumActiveListings;

				if (listings == null) {
					listings = new Dictionary<ulong, JsonObject>((int)totalListings);
				}

				foreach (JsonNode? listing in marketListings.Listings) {
					if (listing == null) {
						bot.ArchiLogger.LogNullError(listing);
						
						return null;
					}

					ulong? listingid = listing["listingid"]?.ToString().ToJsonObject<ulong>();
					if (listingid == null) {
						bot.ArchiLogger.LogNullError(listingid);
						
						return null;
					}
					
					if (listings.TryAdd(listingid.Value, listing.AsObject())) {
						listingsCollected++;
					}
				}
			} while (listingsCollected < totalListings);

			return listings;
		}

		private static async Task<Dictionary<string, List<ulong>>?> GetListingIDsFromIdentifiers(Bot bot, List<ItemIdentifier> itemIdentifiers) {
			Dictionary<ulong, JsonObject>? listings = await GetFullMarketListings(bot).ConfigureAwait(false);

			if (listings == null) {
				return null;
			}

			Dictionary<string, List<ulong>> filteredListings = new Dictionary<string, List<ulong>>();
			foreach ((ulong listingID, JsonObject listing) in listings) {
				ItemListing item;
				try {
					item = new ItemListing(listing);
				} catch (Exception e) {
					bot.ArchiLogger.LogGenericException(e);

					return null;
				}

				foreach (ItemIdentifier itemIdentifier in itemIdentifiers) {
					if (itemIdentifier.IsItemListingMatch(item)) {
						if (!filteredListings.ContainsKey(itemIdentifier.ToString())) {
							filteredListings.Add(itemIdentifier.ToString(), new List<ulong>());
						}

						filteredListings[itemIdentifier.ToString()].Add(listingID);
					}
				}
			}

			return filteredListings;
		}

		internal static async Task<string> GetBuyLimit(Bot bot) {
			(Steam.MarketListingsResponse? marketListings, _) = await WebRequest.GetMarketListings(bot).ConfigureAwait(false);

			if (marketListings == null || !marketListings.Success) {
				return Strings.MarketListingsFetchFailed;
			}

			long buyOrderValue = 0;
			foreach (JsonNode? listing in marketListings.BuyOrders) {
				if (listing == null) {
					bot.ArchiLogger.LogNullError(listing);
						
					return Strings.MarketListingsFetchFailed;
				}

				uint? quantity_remaining = listing["quantity_remaining"]?.ToString().ToJsonObject<uint>();
				if (quantity_remaining == null) {
					bot.ArchiLogger.LogNullError(quantity_remaining);
						
					return Strings.MarketListingsFetchFailed;
				}

				long? price = listing["price"]?.ToString().ToJsonObject<long>();
				if (price == null) {
					bot.ArchiLogger.LogNullError(price);
						
					return Strings.MarketListingsFetchFailed;
				}

				buyOrderValue += price.Value * quantity_remaining.Value;					
			}

			long buyOrderLimit = bot.WalletBalance * 10;
			long remainingBuyOrderLimit = buyOrderValue > buyOrderLimit ? 0 : buyOrderLimit - buyOrderValue;
			double buyOrderUsagePercent = buyOrderLimit == 0 ? (buyOrderValue == 0 ? 1 : Double.PositiveInfinity) : (double) buyOrderValue / buyOrderLimit;

			return Commands.FormatBotResponse(bot, String.Format(Strings.MarketBuyLimit, String.Format("{0:#,#0.00}", buyOrderValue / 100.0), String.Format("{0:#,#0.00}", buyOrderLimit / 100.0), String.Format("{0:0%}", buyOrderUsagePercent), String.Format("{0:#,#0.00}", remainingBuyOrderLimit / 100.0), bot.WalletCurrency.ToString()));
		}

		internal static bool StopMarketRepeatTimer(Bot bot) {
			if (!MarketRepeatTimers.ContainsKey(bot)) {
				return false;
			}

			if (MarketRepeatTimers.TryRemove(bot, out (Timer, StatusReporter?) item)) {
				(Timer? oldTimer, StatusReporter? statusReporter) = item;
				
				if (oldTimer != null) {
					oldTimer.Change(Timeout.Infinite, Timeout.Infinite);
					oldTimer.Dispose();
				}

				if (statusReporter != null) {
					statusReporter.ForceSend();
				}
			}

			return true;
		}

		internal static void StartMarketRepeatTimer(Bot bot, uint minutes, StatusReporter? statusReporter) {
			StopMarketRepeatTimer(bot);

			Timer newTimer = new Timer(async _ => await MarketHandler.AcceptMarketConfirmations(bot, statusReporter).ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite);
			if (MarketRepeatTimers.TryAdd(bot, (newTimer, statusReporter))) {
				newTimer.Change(TimeSpan.FromMinutes(minutes), TimeSpan.FromMinutes(minutes));
			} else {
				newTimer.Dispose();
			}
		}

		private static async Task AcceptMarketConfirmations(Bot bot, StatusReporter? statusReporter) {
			(bool success, _, string message) = await bot.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Market).ConfigureAwait(false);

			string report = success ? message : String.Format(ArchiSteamFarm.Localization.Strings.WarningFailedWithError, message);
			if (statusReporter != null) {
				statusReporter.Report(bot, report);
			} else {
				bot.ArchiLogger.LogGenericInfo(report);
			}
		}

		internal static void StartMarketAlertTimer() {
			MarketAlertTimer.Change(MarketAlertUpdateFrequence, MarketAlertUpdateFrequence);
		}

		internal static async Task CheckMarketAlerts() {
			HashSet<uint> nameIDsToPriceCheck = BoosterHandler.BoosterHandlers.Values.SelectMany(handler => handler.BotCache.MarketAlerts.Select(alert => alert.NameID)).ToHashSet();
			foreach (uint nameID in nameIDsToPriceCheck) {
				// Select a single bot for each currency we want to do a price check on
				List<Bot?> bots = BoosterHandler.BoosterHandlers.Values.Where(handler => handler.BotCache.MarketAlerts.FirstOrDefault(alert => alert.NameID == nameID) != null).Select(handler => handler.Bot).GroupBy(bot => bot.WalletCurrency).Select(group => group.FirstOrDefault(bot => bot.IsConnectedAndLoggedOn)).ToList();
				foreach (Bot? bot in bots) {
					if (bot == null) {
						// No bots are online to check this currency
						continue;
					}
					
					JsonDocument? marketPriceHistogramResponse = await WebRequest.GetMarketPriceHistogram(bot, nameID).ConfigureAwait(false);
					if (marketPriceHistogramResponse == null) {
						ASF.ArchiLogger.LogGenericError(Strings.PriceHistogramFetchFailed);
						continue;
					}

					Steam.ItemOrdersHistogramResponse? marketPriceHistogram = marketPriceHistogramResponse.ToJsonText().ToJsonObject<Steam.ItemOrdersHistogramResponse>();
					if (marketPriceHistogram == null || marketPriceHistogram.Success != 1) {
						ASF.ArchiLogger.LogGenericError(String.Format(Strings.ErrorBadSuccessResponse, nameof(marketPriceHistogram.Success), marketPriceHistogram?.Success));
						continue;
					}

					// Steam uses 2 decimal places for all currencies here, regardless of how many that currency actually uses
					// https://en.wikipedia.org/wiki/ISO_4217#List_of_ISO_4217_currency_codes
					// https://github.com/SteamRE/SteamKit/blob/4801f739bf7958e1a7cd2f63d72bd310f49fe864/SteamKit2/SteamKit2/Base/Generated/SteamLanguage.cs#L2822

					uint? sellNowPrice = null;
					if (marketPriceHistogram.BuyOrderGraph.Count != 0) {
						try {
							sellNowPrice = (uint) (marketPriceHistogram.BuyOrderGraph[0][0].ToJsonObject<decimal>() * 100);
						} catch (Exception e) {
							ASF.ArchiLogger.LogGenericException(e);
							continue;
						}
					}

					uint? buyNowPrice = null;
					if (marketPriceHistogram.SellOrderGraph.Count != 0) {
						try {
							buyNowPrice = (uint) (marketPriceHistogram.SellOrderGraph[0][0].ToJsonObject<decimal>() * 100);
						} catch (Exception e) {
							ASF.ArchiLogger.LogGenericException(e);
							continue;
						}
					}

					List<(MarketAlert alert, Bot alertBot)> alerts = BoosterHandler.BoosterHandlers.Values.Where(handler => handler.Bot.WalletCurrency == bot.WalletCurrency).SelectMany(handler => handler.BotCache.MarketAlerts.Where(alert => alert.NameID == nameID).Select(alert => (alert, handler.Bot))).ToList();
					foreach ((MarketAlert alert, Bot alertBot) in alerts) {
						alert.CheckAlert(alertBot, buyNowPrice, sellNowPrice);
					}
				}
			}
		}

		internal static async Task<MarketAlert?> CreateMarketAlert(Bot bot, uint appID, string hashName, MarketAlertType type, MarketAlertMode mode, uint amount, StatusReporter statusReporter) {
			ArgumentNullException.ThrowIfNull(BoosterManager.GlobalCache);

			uint nameID;
			if (BoosterManager.GlobalCache.TryGetNameID(appID, hashName, out uint outValue)) {
				nameID = outValue;
			} else {
				MarketListingPageResponse? marketListingPage = await WebRequest.GetMarketListing(bot, appID, hashName);
				if (marketListingPage == null) {
					bot.ArchiLogger.LogNullError(marketListingPage);
					
					return null;
				}

				nameID = marketListingPage.NameID;
				BoosterManager.GlobalCache.SetNameID(appID, hashName, nameID);
			}

			BotCache botCache = BoosterHandler.BoosterHandlers[bot.BotName].BotCache;
			MarketAlert marketAlert = new MarketAlert(appID, hashName, nameID, type, mode, amount, statusReporter);
			if (!botCache.AddMarketAlert(marketAlert)) {
				bot.ArchiLogger.LogGenericError(Strings.MarketAlertAlreadyExists);

				return null;
			}

			return marketAlert;
		}

		internal static IEnumerable<MarketAlert> DeleteMarketAlerts(Bot bot, uint? appID = null, string? hashName = null, MarketAlertType? type = null, MarketAlertMode? mode = null, uint? amount = null) {
			BotCache botCache = BoosterHandler.BoosterHandlers[bot.BotName].BotCache;
			HashSet<MarketAlert> alertsToRemove = botCache.MarketAlerts.Where(alert => {
				if (appID != null && alert.AppID != appID) {
					return false;
				}

				if (hashName != null && alert.HashName != hashName) {
					return false;
				}

				if (type != null && alert.Type != type) {
					return false;
				}

				if (mode != null && alert.Mode != mode) {
					return false;
				}

				if (amount != null && alert.Amount != amount) {
					return false;
				}

				return true;
			}).ToHashSet();

			foreach (MarketAlert alert in alertsToRemove) {
				botCache.RemoveMarketAlert(alert);
			}

			return alertsToRemove;
		}

		internal static string PrintMarketAlerts(Bot bot, IEnumerable<MarketAlert>? alerts = null, bool showCancelCommand = false) {
			if (alerts == null) {
				alerts = BoosterHandler.BoosterHandlers[bot.BotName].BotCache.MarketAlerts;
			}

			if (alerts.Count() == 0) {
				return Strings.MarketAlertsEmpty;
			}

			return String.Join(Environment.NewLine, alerts.Order(new MarketAlertComparer()).Select(alert => {
				string typeString = alert.Type switch {
					MarketAlertType.Buy => Strings.MarketAlertTypeBuy,
					MarketAlertType.Sell => Strings.MarketAlertTypeSell,
					_ => throw new ArgumentOutOfRangeException()
				};

				string modeString = alert.Mode switch {
					MarketAlertMode.Above => Strings.MarketAlertModeAbove,
					MarketAlertMode.Below => Strings.MarketAlertModeBelow,
					MarketAlertMode.AboveOrAt => Strings.MarketAlertModeAboveOrAt,
					MarketAlertMode.BelowOrAt => Strings.MarketAlertModeBelowOrAt,
					_ => throw new ArgumentOutOfRangeException()
				};

				return String.Format(showCancelCommand ? Strings.MarketAlertWithCancelCommand : Strings.MarketAlert,
					alert.AppID,
					alert.HashName,
					typeString,
					modeString,
					String.Format(CultureInfo.CurrentCulture, "{0:#,#0.00}", alert.Amount / 100.0),
					bot.WalletCurrency,
					String.Format("!cma {0} {1} {2} {3} {4} {5}",
						bot.BotName,
						alert.AppID,
						Uri.EscapeDataString(alert.HashName),
						alert.Type.ToString(),
						alert.Mode.ToString(),
						String.Format(CultureInfo.CurrentCulture, "{0:#,#0.00}", alert.Amount / 100.0)
					)
				);
			}));
		}
	}
}
