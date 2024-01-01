using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;

namespace BoosterManager {
	internal static class InventoryHandler {
		internal static async Task<string> SendItemToMultipleBots(Bot sender, List<(Bot reciever, uint amount)> recievers, uint appID, ulong contextID, ItemIdentifier itemIdentifier) {
			// Send Amounts A,B,... of Item X to Bots C,D,... from Bot E  
			// 	Amount A of Item X to Bot C from Bot E
			// 	Amount B of Item X to Bot D from Bot E

			HashSet<Asset> itemStacks;
			try {
				itemStacks = await sender.ArchiWebHandler.GetInventoryAsync(appID: appID, contextID: contextID).Where(item => itemIdentifier.IsItemMatch(item)).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				sender.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(sender, Strings.WarningFailed);
			}

			HashSet<string> responses = new HashSet<string>();
			uint totalAmountSent = 0;

			foreach ((Bot reciever, uint amount) in recievers) {
				sender.ArchiLogger.LogGenericInfo(String.Format("Sending {0} of {1} to {2}", amount, itemIdentifier.ToString(), reciever.BotName));
				(bool success, string response) = await SendItem(sender, itemStacks, reciever, amount, totalAmountSent).ConfigureAwait(false);
				responses.Add(Commands.FormatBotResponse(reciever, response));

				if (success) {
					sender.ArchiLogger.LogGenericInfo(String.Format("Sent {0} of {1} to {2}", amount, itemIdentifier.ToString(), reciever.BotName));
					totalAmountSent += amount;
				} else {
					sender.ArchiLogger.LogGenericError(String.Format("Failed to send {0} of {1} to {2}", amount, itemIdentifier.ToString(), reciever.BotName));
				}
			}

			return String.Join(Environment.NewLine, responses);
		}

		private static async Task<(bool, String)> SendItem(Bot sender, HashSet<Asset> itemStacks, Bot reciever, uint amountToSend, uint amountToSkip = 0) {
			if (!reciever.IsConnectedAndLoggedOn) {
				return (false, Strings.BotNotConnected);
			}

			if (sender.SteamID == reciever.SteamID) {
				return (false, Strings.BotSendingTradeToYourself);
			}

			if (amountToSend == 0) {
				return (true, "Successfully sent nothing!");
			}

			HashSet<Asset>? itemsToGive = GetItemsFromStacks(sender, itemStacks, amountToSend, amountToSkip);
			if (itemsToGive == null) {
				return (false, "Not enough to send!");
			}

			(bool success, _, HashSet<ulong>? mobileTradeOfferIDs) = await sender.ArchiWebHandler.SendTradeOffer(reciever.SteamID, itemsToGive).ConfigureAwait(false);
			if ((mobileTradeOfferIDs?.Count > 0) && sender.HasMobileAuthenticator) {
				(bool twoFactorSuccess, _, string message) = await sender.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

				if (!twoFactorSuccess) {
					sender.ArchiLogger.LogGenericError(message);
					return (success, Strings.BotLootingFailed);
				}
			}

			return (success, success ? Strings.BotLootingSuccess : Strings.BotLootingFailed);
		}

		internal static async Task<string> SendMultipleItemsToMultipleBots(Bot sender, HashSet<Bot> recievers, uint appID, ulong contextID, List<(ItemIdentifier itemIdentifier, uint amount)> items) {
			// Send Amounts A,B,... of Items X,Y,... to Bots C,D,... from Bot E
			// 	Amount A of Item X to Bot C from Bot E
			// 	Amount B of Item Y to Bot C from Bot E
			// 	Amount A of Item X to Bot D from Bot E
			// 	Amount B of Item Y to Bot D from Bot E

			HashSet<Asset> inventory;
			try {
				inventory = await sender.ArchiWebHandler.GetInventoryAsync(appID: appID, contextID: contextID).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				sender.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(sender, Strings.WarningFailed);
			}

			// Link each inventory Asset to a matching ItemIdentifier
			//	Note: It's possible that, depending on what ItemIdentifiers are used, some Assets can match with more than one ItemIdentifier.
			//	If allowed to happen, we might end up trying to trade the same item multiple times.
			List<(HashSet<Asset> itemStacks, ItemIdentifier itemIdentifier, uint amount)> itemStacksWithAmounts = new List<(HashSet<Asset>, ItemIdentifier, uint)>();
			HashSet<ulong> allAssetIDs = new HashSet<ulong>(); // Used to ensure that no Asset exists in more than one itemStack.  Assets will only be linked to the first matching ItemIdentifier.
			foreach ((ItemIdentifier itemIdentifier, uint amount) in items) {
				HashSet<Asset> itemStacks = inventory.Where(item => item.Tradable && itemIdentifier.IsItemMatch(item) && !allAssetIDs.Contains(item.AssetID)).ToHashSet();
				allAssetIDs.UnionWith(itemStacks.Select(x => x.AssetID));
				itemStacksWithAmounts.Add((itemStacks, itemIdentifier, amount));
			}

			// Send the trades
			HashSet<string> responses = new HashSet<string>();
			uint numRecieversProcessed = 0;

			foreach (Bot reciever in recievers) {
				(bool success, string response) = await SendMultipleItems(sender, reciever, itemStacksWithAmounts, numRecieversProcessed).ConfigureAwait(false);
				responses.Add(Commands.FormatBotResponse(reciever, response));

				if (success) {
					sender.ArchiLogger.LogGenericInfo(String.Format("Sent items to {0}", reciever.BotName));
					numRecieversProcessed++;
				} else {
					sender.ArchiLogger.LogGenericError(String.Format("Failed to send items to {0}", reciever.BotName));
				}
			}

			return String.Join(Environment.NewLine, responses);
		}

		private static async Task<(bool, String)> SendMultipleItems(Bot sender, Bot reciever, List<(HashSet<Asset> itemStacks, ItemIdentifier itemIdentifier, uint amount)> itemStacksWithAmounts, uint numRecieversProcessed = 0) {
			if (!reciever.IsConnectedAndLoggedOn) {
				return (false, Strings.BotNotConnected);
			}

			if (sender.SteamID == reciever.SteamID) {
				return (false, Strings.BotSendingTradeToYourself);
			}

			HashSet<Asset> totalItemsToGive = new HashSet<Asset>();
			HashSet<string> responses = new HashSet<string>();
			bool completeSuccess = true;

			// Allow for partial trades, but not partial amounts of individual items.
			// If user is trying to send: 3 of ItemA and 2 of ItemB.  Yet they have: 3 of ItemA and 1 of ItemB.  This will send only: 3 of ItemA and 0 of ItemB
			foreach ((HashSet<Asset> itemStacks, ItemIdentifier itemIdentifier, uint amount) in itemStacksWithAmounts) {
				HashSet<Asset>? itemsToGive = GetItemsFromStacks(sender, itemStacks, amount, amount * numRecieversProcessed);
				if (itemsToGive == null) {
					sender.ArchiLogger.LogGenericInfo(String.Format("Not enough of {0} to send!", itemIdentifier.ToString()));
					responses.Add(String.Format("Not enough of {0} to send :steamthumbsdown:", itemIdentifier.ToString()));
					completeSuccess = false;
					continue;
				}

				responses.Add(String.Format("Successfully sent {0} of {1} :steamthumbsup:", amount, itemIdentifier.ToString()));
				totalItemsToGive.UnionWith(itemsToGive);
			}

			if (totalItemsToGive.Count() == 0) {
				return (false, String.Join(Environment.NewLine, responses));
			}

			(bool success, _, HashSet<ulong>? mobileTradeOfferIDs) = await sender.ArchiWebHandler.SendTradeOffer(reciever.SteamID, totalItemsToGive).ConfigureAwait(false);
			if ((mobileTradeOfferIDs?.Count > 0) && sender.HasMobileAuthenticator) {
				(bool twoFactorSuccess, _, string message) = await sender.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EConfirmationType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

				if (!twoFactorSuccess) {
					sender.ArchiLogger.LogGenericError(message);
					return (success, Strings.BotLootingFailed);
				}
			}

			if (!success) {
				return (success, Strings.BotLootingFailed);
			}

			if (!completeSuccess) {
				return (success, String.Join(Environment.NewLine, responses));
			}

			return (success, Strings.BotLootingSuccess);
		}

		private static HashSet<Asset>? GetItemsFromStacks(Bot bot, HashSet<Asset> itemStacks, uint amountToTake, uint amountToSkip) {
			HashSet<Asset> items = new HashSet<Asset>();	
			uint amountTaken = 0;
			uint itemCount = 0;

			foreach (Asset itemStack in itemStacks) {
				itemCount += itemStack.Amount;
				
				uint amountLeftInStack = Math.Min((amountToSkip > itemCount) ? 0 : (itemCount - amountToSkip), itemStack.Amount);
				if (amountLeftInStack == 0) {
					continue;
				}

				uint amountToTakeFromStack = Math.Min(Math.Min(itemStack.Amount, amountLeftInStack), amountToTake - amountTaken);
				if (amountToTakeFromStack == 0) {
					break;
				}

				items.Add(new Asset(appID: itemStack.AppID, contextID: itemStack.ContextID, classID: itemStack.ClassID, assetID: itemStack.AssetID, amount: amountToTakeFromStack));
				amountTaken += amountToTakeFromStack;
			}

			if (items.Count == 0 || amountTaken != amountToTake) {
				bot.ArchiLogger.LogGenericError(String.Format("Not enough available quantity to complete trade, need {0}, but only {1} are available", amountToTake, amountTaken));

				return null;
			}

			return items;
		}
	}
}
