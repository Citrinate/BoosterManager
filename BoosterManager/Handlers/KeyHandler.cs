using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;

namespace BoosterManager {
	internal static class KeyHandler {
		internal const uint KeyAppID = 440;
		internal const ulong KeyContextID = 2;
		internal const string MarketHash = "Mann Co. Supply Crate Key";

		internal static async Task<string> GetKeyCount(Bot bot) {
			HashSet<Asset> inventory;
			try {
				inventory = await bot.ArchiWebHandler.GetInventoryAsync(appID: KeyAppID, contextID: KeyContextID).Where(item => ItemIdentifier.KeyIdentifier.IsItemMatch(item)).ToHashSetAsync().ConfigureAwait(false);
			} catch (Exception e) {
				bot.ArchiLogger.LogGenericException(e);
				return Commands.FormatBotResponse(bot, Strings.WarningFailed);
			}

			(uint tradable, uint untradable) keys = (0,0);

			foreach (Asset item in inventory) {
				if (item.Tradable) {
					keys.tradable += item.Amount;
				} else {
					keys.untradable += item.Amount;
				}
			}

			return Commands.FormatBotResponse(bot, String.Format("Tradable: {0:N0}{1}", keys.tradable,
				(keys.untradable) == 0 ? "" : String.Format("; Untradable: {0:N0}", keys.untradable))
			);
		}
	}
}
