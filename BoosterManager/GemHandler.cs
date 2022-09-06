using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Security;

namespace BoosterManager {
	internal sealed class GemHandler {
		internal const ulong GemsClassID = 667924416;
		internal const ulong SackOfGemsClassID = 667933237; 

		internal static async Task<string> GetGemCount(Bot bot) {
			HashSet<Asset> inventory;
			try {
				inventory = await bot.ArchiWebHandler.GetInventoryAsync().Where(item => item.Type == Asset.EType.SteamGems).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(bot, Strings.WarningFailed);
			}

			(uint tradable, uint untradable) gems = (0,0);
			(uint tradable, uint untradable) sacks = (0,0);
			foreach (Asset item in inventory) {
				switch (item.ClassID, item.Tradable) {
					case (GemsClassID, true): gems.tradable += item.Amount; break;
					case (GemsClassID, false): gems.untradable += item.Amount; break;
					case (SackOfGemsClassID, true): sacks.tradable += item.Amount; break;
					case (SackOfGemsClassID, false): sacks.untradable += item.Amount; break;
					default: break;
				}
			}

			return Commands.FormatBotResponse(bot, String.Format("Tradable: {0:N0}{1}{2}", gems.tradable, sacks.tradable == 0 ? "" : String.Format(" (+{0:N0} Sacks)", sacks.tradable),
				(gems.untradable + sacks.untradable) == 0 ? "" : String.Format("; Untradable: {0:N0}{1}", gems.untradable, sacks.untradable == 0 ? "" : String.Format(" (+{0:N0} Sacks)", sacks.untradable))
			));
		}

		internal static async Task<string> TransferGems(Bot sender, List<(Bot reciever, uint amount)> recievers) {
			HashSet<Asset> gemsStacks;
			try {
				gemsStacks = await sender.ArchiWebHandler.GetInventoryAsync().Where(item => item.Tradable && item.ClassID == GemsClassID).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				sender.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(sender, Strings.WarningFailed);
			}

			HashSet<string> responses = new HashSet<string>();
			uint totalGemsTransfered = 0;
			foreach ((Bot reciever, uint amount) in recievers) {
				sender.ArchiLogger.LogGenericInfo(String.Format("Sending {0} gems to {1}", amount, reciever.BotName));
				(bool success, string response) = await SendFungibleItems(sender, gemsStacks, reciever, amount, totalGemsTransfered).ConfigureAwait(false);
				responses.Add(Commands.FormatBotResponse(reciever, response));
				if (success) {
					sender.ArchiLogger.LogGenericInfo(String.Format("Sent {0} gems to {1}", amount, reciever.BotName));
					totalGemsTransfered += amount;
				} else {
					sender.ArchiLogger.LogGenericError(String.Format("Failed to send {0} gems to {1}", amount, reciever.BotName));
				}
			}

			return String.Join(Environment.NewLine, responses);
		}

		private static async Task<(bool, String)> SendFungibleItems(Bot sender, HashSet<Asset> itemStacks, Bot reciever, uint amountToSend, uint amountToSkip = 0) {
			if (!reciever.IsConnectedAndLoggedOn) {
				return (false, Strings.BotNotConnected);
			}

			if (amountToSend == 0) {
				return (true, "Successfully sent nothing!");
			}

			HashSet<Asset> itemsToGive = new HashSet<Asset>();	
			uint amountSent = 0;
			uint itemCount = 0;
			foreach (Asset itemStack in itemStacks) {
				itemCount += itemStack.Amount;
				uint amountLeftInStack = Math.Min((amountToSkip > itemCount) ? 0 : (itemCount - amountToSkip), itemStack.Amount);
				if (amountLeftInStack == 0) {
					continue;
				}

				uint amountToSendFromStack = Math.Min(Math.Min(itemStack.Amount, amountLeftInStack), amountToSend - amountSent);
				if (amountToSendFromStack == 0) {
					break;
				}

				itemsToGive.Add(new Asset(appID: itemStack.AppID, contextID: itemStack.ContextID, classID: itemStack.ClassID, assetID: itemStack.AssetID, amount: amountToSend));
				amountSent += amountToSendFromStack;
			}
			if (itemsToGive.Count == 0 || amountSent != amountToSend) {
				sender.ArchiLogger.LogGenericError(String.Format("Not enough available quantity to complete trade, need {0}, but only {1} are available", amountToSend, amountSent));
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
	}
}
