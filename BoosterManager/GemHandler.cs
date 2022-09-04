using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;

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
                inventory = await bot.ArchiWebHandler.GetInventoryAsync().ToHashSetAsync().ConfigureAwait(false);
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
                }
            }

            return Commands.FormatBotResponse(bot, String.Format("Tradable: {0:N0} (+{1:N0} Sacks); Untradable: {2:N0} (+{3:N0} Sacks)", tradableGemCount, tradableSackCount, untradableGemCount, untradableSackCount));
        }
    }
}