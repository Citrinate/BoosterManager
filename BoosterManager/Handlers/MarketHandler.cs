using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using Newtonsoft.Json.Linq;

namespace BoosterManager {
	internal static class MarketHandler {
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
				uint? price = listing["price"]?.ToObject<uint>();
				if (price == null) {
					bot.ArchiLogger.LogNullError(price);

					return null;
				}

				listingsValue += price.Value;
			}

			return listingsValue;
		}

		internal static async Task<string> FindListings(Bot bot, List<string> itemNames) {
			Dictionary<string, List<ulong>>? filteredListings = await GetListingIDsFromName(bot, itemNames).ConfigureAwait(false);

			if (filteredListings == null) {
				return Commands.FormatBotResponse(bot, "Failed to load Market Listings");
			}

			if (filteredListings.Count == 0) {
				return Commands.FormatBotResponse(bot, "No listings found");
			}

			List<string> responses = new List<string>();
			foreach ((string itemName, List<ulong> listingIDs) in filteredListings) {
				responses.Add(String.Format("{0} listings found for {1}: {2}", listingIDs.Count, itemName, String.Join(", ", listingIDs)));
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
				return Commands.FormatBotResponse(bot, String.Format("Cancelled {0}/{1} listings, failed to cancel: {2}", listingIDs.Count - failedListingIDs.Count, listingIDs.Count, String.Join(", ", failedListingIDs)));
			}

			return Commands.FormatBotResponse(bot, String.Format("Removed {0} listings", listingIDs.Count));
		}

		internal static async Task<string> FindAndRemoveListings(Bot bot, List<string> itemNames) {
			Dictionary<string, List<ulong>>? filteredListings = await GetListingIDsFromName(bot, itemNames).ConfigureAwait(false);

			if (filteredListings == null) {
				return Commands.FormatBotResponse(bot, "Failed to load Market Listings");
			}

			if (filteredListings.Count == 0) {
				return Commands.FormatBotResponse(bot, "No listings found");
			}

			List<ulong> listingIDs = filteredListings.Values.SelectMany(x => x).Distinct().ToList();

			return await RemoveListings(bot, listingIDs).ConfigureAwait(false);
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
					ulong? listingid = listing["listingid"]?.ToObject<ulong>();
					if (listingid == null) {
						bot.ArchiLogger.LogNullError(listingid);
						
						return null;
					}
					
					if (listings.TryAdd(listingid.Value, listing)) {
						listingsCollected++;
					}
				}
			} while (listingsCollected < totalListings);

			return listings;
		}

		private static async Task<Dictionary<string, List<ulong>>?> GetListingIDsFromName(Bot bot, List<string> itemNames) {
			Dictionary<ulong, JObject>? listings = await GetFullMarketListings(bot).ConfigureAwait(false);

			if (listings == null) {
				return null;
			}

			Dictionary<string, List<ulong>> filteredListings = new Dictionary<string, List<ulong>>();
			List<ulong> listingIDs = new List<ulong>();
			foreach ((ulong listingID, JObject listing) in listings) {
				string? name = listing["asset"]?["name"]?.ToString();
				if (name == null) {
					bot.ArchiLogger.LogNullError(name);

					return null;
				}

				string? marketName = listing["asset"]?["market_name"]?.ToString();
				if (marketName == null) {
					bot.ArchiLogger.LogNullError(marketName);

					return null;
				}

				foreach (string itemName in itemNames) {
					if (name.Equals(itemName) || marketName.Equals(itemName)) {
						if (!filteredListings.ContainsKey(itemName)) {
							filteredListings.Add(itemName, new List<ulong>());
						}

						filteredListings[itemName].Add(listingID);
					}
				}
			}

			return filteredListings;
		}
	}
}
