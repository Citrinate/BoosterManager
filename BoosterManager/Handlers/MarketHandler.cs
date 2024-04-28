using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using BoosterManager.Localization;

namespace BoosterManager {
	internal static class MarketHandler {
		private static ConcurrentDictionary<Bot, (Timer, StatusReporter?)> MarketRepeatTimers = new();

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

			if (subtractFrom != 0) {
				return Commands.FormatBotResponse(bot, String.Format(Strings.AccountValueRemaining, String.Format("{0:#,#0.00}", subtractFrom - ((listingsValue + bot.WalletBalance) / 100.0)), bot.WalletCurrency.ToString()));
			}

			return Commands.FormatBotResponse(bot, String.Format(Strings.AccountValue, String.Format("{0:#,#0.00}", (listingsValue + bot.WalletBalance) / 100.0), bot.WalletCurrency.ToString()));
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
				await Task.Delay(100).ConfigureAwait(false);
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
				await Task.Delay(100).ConfigureAwait(false);
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

		internal static async Task<bool> StopMarketRepeatTimer(Bot bot) {
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
					await statusReporter.Send().ConfigureAwait(false);
				}
			}

			return true;
		}

		internal static async Task StartMarketRepeatTimer(Bot bot, uint minutes, StatusReporter? statusReporter) {
			await StopMarketRepeatTimer(bot).ConfigureAwait(false);

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
	}
}
