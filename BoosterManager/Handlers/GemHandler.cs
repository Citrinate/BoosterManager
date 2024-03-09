using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;

namespace BoosterManager {
	internal static class GemHandler {
		internal const ulong GemsClassID = 667924416;
		internal const ulong SackOfGemsClassID = 667933237;

		internal static async Task<string> GetGemCount(Bot bot) {
			HashSet<Asset> inventory;
			try {
				inventory = await bot.ArchiWebHandler.GetInventoryAsync().Where(item => ItemIdentifier.GemAndSackIdentifier.IsItemMatch(item)).ToHashSetAsync().ConfigureAwait(false);
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

		internal static async Task<string> UnpackGems(Bot bot) {
			HashSet<Asset> sacks;
			try {
				sacks = await bot.ArchiWebHandler.GetInventoryAsync().Where(item => ItemIdentifier.SackIdentifier.IsItemMatch(item) && (BoosterHandler.AllowCraftUntradableBoosters || item.Tradable)).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(bot, Strings.WarningFailed);
			}

			if (sacks.Count == 0) {
				return Commands.FormatBotResponse(bot, "No gems to unpack");
			}

			foreach (Asset sack in sacks) {
				Steam.ExchangeGooResponse? response = await WebRequest.UnpackGems(bot, sack.AssetID, sack.Amount);

				if (response == null || response.Success != 1) {
					return Commands.FormatBotResponse(bot, Strings.WarningFailed);
				}
			}

			return Commands.FormatBotResponse(bot, Strings.Success);
		}
	}
}
