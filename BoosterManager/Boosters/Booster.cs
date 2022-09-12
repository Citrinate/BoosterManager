using ArchiSteamFarm.Steam;
using SteamKit2;
using System;
using System.Threading.Tasks;

namespace BoosterManager {
	internal sealed class Booster {
		private readonly Bot Bot;
		internal readonly uint GameID;
		internal readonly BoosterType Type;
		internal readonly Steam.BoosterInfo Info;
		private readonly DateTime InitTime;
		internal bool WasCrafted = false;

		internal Booster(Bot bot, uint gameID, BoosterType type, Steam.BoosterInfo info) {
			Bot = bot;
			GameID = gameID;
			Type = type;
			Info = info;
			InitTime = DateTime.Now;
		}

		internal async Task<Steam.BoostersResponse?> Craft(TradabilityPreference nTp) {
			Steam.BoostersResponse? result = await WebRequest.CreateBooster(Bot, Info.AppID, Info.Series, nTp).ConfigureAwait(false);
			if (result?.Result?.Result == EResult.OK) {
				WasCrafted = true;
			}

			return result;
		}

		internal DateTime GetAvailableAtTime() {
			if (Info.Unavailable && Info.AvailableAtTime != null) {
				// Unavailable boosters become available exactly 24 hours after being crafted, down to the second, but Steam doesn't tell us which second that is.
				// Rounding up to the next minute allows us to safely assume that our boosters will be available when we attempt to craft them.
				// TODO: When crafting with the plugin, save the time we crafted at, so we'll have something that's more accurate
				return Info.AvailableAtTime.Value.AddMinutes(1);
			}

			return InitTime;
		}
	}
}