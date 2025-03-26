using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using BoosterManager.Localization;

namespace BoosterManager {
	internal static class GemHandler {
		internal const ulong GemsClassID = 667924416;
		internal const ulong SackOfGemsClassID = 667933237;

		internal static async Task<string> GetGemCount(Bot bot) {
			HashSet<Asset> inventory;
			try {
				inventory = await bot.ArchiHandler.GetMyInventoryAsync().Where(item => ItemIdentifier.GemAndSackIdentifier.IsItemMatch(item)).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.WarningFailed);
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

			string response = String.Format(Strings.ItemsCountTradable, String.Format("{0:N0}", gems.tradable));

			if (sacks.tradable > 0) {
				response += String.Format(" (+{0})", String.Format(Strings.GemSacksCount, String.Format("{0:N0}", sacks.tradable)));
			}

			if (gems.untradable + sacks.untradable > 0) {
				response += String.Format("; {0}", String.Format(Strings.ItemsCountUntradable, String.Format("{0:N0}", gems.untradable)));
				
				if (sacks.untradable > 0) {
					response += String.Format(" (+{0})", String.Format(Strings.GemSacksCount, String.Format("{0:N0}", sacks.untradable)));
				}
			}

			return Commands.FormatBotResponse(bot, response);
		}

		internal static async Task<string> UnpackGems(Bot bot) {
			HashSet<Asset> sacks;
			try {
				sacks = await bot.ArchiHandler.GetMyInventoryAsync().Where(item => ItemIdentifier.SackIdentifier.IsItemMatch(item) && (BoosterHandler.AllowCraftUntradableBoosters || item.Tradable)).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.WarningFailed);
			}

			if (sacks.Count == 0) {
				return Commands.FormatBotResponse(bot, Strings.NoSacksFound);
			}

			foreach (Asset sack in sacks) {
				Steam.ExchangeGooResponse? response = await WebRequest.ExchangeGoo(bot, sack.AssetID, sack.Amount, pack: true);

				if (response == null || response.Success != 1) {
					return Commands.FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.WarningFailed);
				}
			}

			return Commands.FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.Success);
		}

		internal static async Task<string> PackGems(Bot bot) {
			HashSet<Asset> gems;
			try {
				gems = await bot.ArchiHandler.GetMyInventoryAsync().Where(item => ItemIdentifier.GemIdentifier.IsItemMatch(item) && (BoosterHandler.AllowCraftUntradableBoosters || item.Tradable) && item.Amount >= 1000).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.WarningFailed);
			}

			if (gems.Count == 0) {
				return Commands.FormatBotResponse(bot, Strings.UnpackNoGemsFound);
			}

			foreach (Asset gem in gems) {
				Steam.ExchangeGooResponse? response = await WebRequest.ExchangeGoo(bot, gem.AssetID, gem.Amount, pack: false);

				if (response == null || response.Success != 1) {
					return Commands.FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.WarningFailed);
				}
			}

			return Commands.FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.Success);
		}
	}
}
