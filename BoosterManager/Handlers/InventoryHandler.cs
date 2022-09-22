using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Security;

namespace BoosterManager {
	internal static class InventoryHandler {
		internal static async Task<string> BatchSendItemsWithAmounts(Bot sender, List<(Bot reciever, uint amount)> recievers, uint appID, ulong contextID, Asset.EType type, ulong classID, bool allowUnmarketable = false) {
			HashSet<Asset> itemStacks;
			try {
				itemStacks = await sender.ArchiWebHandler.GetInventoryAsync(appID: appID, contextID: contextID).Where(item => item.Tradable && item.Type == type && item.ClassID == classID && (allowUnmarketable || item.Marketable)).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				sender.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(sender, Strings.WarningFailed);
			}

			HashSet<string> responses = new HashSet<string>();
			uint amountTransfered = 0;

			foreach ((Bot reciever, uint amount) in recievers) {
				sender.ArchiLogger.LogGenericInfo(String.Format("Sending {0} items to {1}", amount, reciever.BotName));
				(bool success, string response) = await SendItemsWithAmounts(sender, itemStacks, reciever, amount, amountTransfered).ConfigureAwait(false);
				responses.Add(Commands.FormatBotResponse(reciever, response));

				if (success) {
					sender.ArchiLogger.LogGenericInfo(String.Format("Sent {0} items to {1}", amount, reciever.BotName));
					amountTransfered += amount;
				} else {
					sender.ArchiLogger.LogGenericError(String.Format("Failed to send {0} items to {1}", amount, reciever.BotName));
				}
			}

			return String.Join(Environment.NewLine, responses);
		}

		private static async Task<(bool, String)> SendItemsWithAmounts(Bot sender, HashSet<Asset> itemStacks, Bot reciever, uint amountToSend, uint amountToSkip = 0) {
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

			(bool success, HashSet<ulong>? mobileTradeOfferIDs) = await sender.ArchiWebHandler.SendTradeOffer(reciever.SteamID, itemsToGive).ConfigureAwait(false);
			if ((mobileTradeOfferIDs?.Count > 0) && sender.HasMobileAuthenticator) {
				(bool twoFactorSuccess, _, string message) = await sender.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

				if (!twoFactorSuccess) {
					sender.ArchiLogger.LogGenericError(message);
					return (success, Strings.BotLootingFailed);
				}
			}

			return (success, success ? Strings.BotLootingSuccess : Strings.BotLootingFailed);
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
