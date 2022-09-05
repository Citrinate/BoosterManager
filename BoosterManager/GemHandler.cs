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
			uint tradableGemCount = 0;
			uint untradableGemCount = 0;
			uint tradableSackCount = 0;
			uint untradableSackCount = 0;
			HashSet<Asset> inventory;
			try {
				inventory = await bot.ArchiWebHandler.GetInventoryAsync().Where(item => item.Type == Asset.EType.SteamGems).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(bot, Strings.WarningFailed);
			}

			foreach (Asset item in inventory) {
				switch (item.ClassID, item.Tradable) {
					case (GemsClassID, true): tradableGemCount += item.Amount; break;
					case (GemsClassID, false): untradableGemCount += item.Amount; break;
					case (SackOfGemsClassID, true): tradableSackCount += item.Amount; break;
					case (SackOfGemsClassID, false): untradableSackCount += item.Amount; break;
					default: break;
				}
			}

			return Commands.FormatBotResponse(bot, String.Format("Tradable: {0:N0}{1}{2}", tradableGemCount, tradableSackCount == 0 ? "" : String.Format(" (+{0:N0} Sacks)", tradableSackCount),
				(untradableGemCount + untradableSackCount) == 0 ? "" : String.Format("; Untradable: {0:N0}{1}", untradableGemCount, untradableSackCount == 0 ? "" : String.Format(" (+{0:N0} Sacks)", untradableSackCount))
			));
		}

		internal static async Task<string> TransferGems(Bot sender, HashSet<Tuple<Bot, uint>> recievers) {
			HashSet<Asset> inventory;
			try {
				inventory = await sender.ArchiWebHandler.GetInventoryAsync().Where(item => item.Tradable && item.ClassID == GemsClassID).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				sender.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(sender, Strings.WarningFailed);
			}

			uint totalGemsTransfered = 0;
			HashSet<string> responses = new HashSet<string>();
			foreach (Tuple<Bot, uint> reciever in recievers) {
				sender.ArchiLogger.LogGenericInfo(String.Format("Sending {0} gems to {1}", reciever.Item2, reciever.Item1.BotName));
				responses.Add(await SendGems(sender, inventory, reciever.Item1, reciever.Item2, totalGemsTransfered).ConfigureAwait(false));
				totalGemsTransfered += reciever.Item2;
			}

			return String.Join(Environment.NewLine, responses);
		}

		private static async Task<String> SendGems(Bot sender, HashSet<Asset> gems, Bot reciever, uint totalAmountToSend, uint amountToSkip = 0) {
			if (totalAmountToSend == 0) {
				return Commands.FormatBotResponse(reciever, "Successfully sent no gems!");
			}

			uint count = 0;
			uint amountSent = 0;
			HashSet<Asset> itemsToGive = new HashSet<Asset>();			
			foreach (Asset gem in gems) {
				count += gem.Amount;
				uint amountLeftInStack = Math.Min(Math.Max(0, count - amountToSkip), gem.Amount);
				if (amountLeftInStack == 0) {
					continue;
				}

				uint amountToSend = Math.Min(Math.Min(gem.Amount, totalAmountToSend - amountSent), amountLeftInStack);
				if (amountToSend == 0) {
					break;
				}

				amountSent += amountToSend;
				itemsToGive.Add(new Asset(appID: gem.AppID, contextID: gem.ContextID, classID: gem.ClassID, assetID: gem.AssetID, amount: amountToSend));
			}
			if (itemsToGive.Count == 0 || count < (totalAmountToSend + amountToSkip)) {
				return Commands.FormatBotResponse(reciever, "Not enough gems.");
			}

			(bool success, HashSet<ulong>? mobileTradeOfferIDs) = await sender.ArchiWebHandler.SendTradeOffer(reciever.SteamID, itemsToGive).ConfigureAwait(false);
			if ((mobileTradeOfferIDs?.Count > 0) && sender.HasMobileAuthenticator) {
				(bool twoFactorSuccess, _, _) = await sender.Actions.HandleTwoFactorAuthenticationConfirmations(true, Confirmation.EType.Trade, mobileTradeOfferIDs, true).ConfigureAwait(false);

				if (!twoFactorSuccess) {
					return Commands.FormatBotResponse(reciever, Strings.BotLootingFailed);
				}
			}

			return Commands.FormatBotResponse(reciever, success ? Strings.BotLootingSuccess : Strings.BotLootingFailed);
		}
	}
}
