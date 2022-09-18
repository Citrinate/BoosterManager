using ArchiSteamFarm.Steam;
using SteamKit2;
using System;
using System.Threading.Tasks;

namespace BoosterManager {
	internal sealed class Booster {
		private readonly Bot Bot;
		private readonly BoosterQueue BoosterQueue;
		internal readonly uint GameID;
		internal readonly BoosterType Type;
		internal readonly Steam.BoosterInfo Info;
		private readonly DateTime InitTime;
		private readonly BoosterLastCraft? LastCraft;
		internal bool WasCrafted = false;

		internal Booster(Bot bot, uint gameID, BoosterType type, Steam.BoosterInfo info, BoosterQueue boosterQueue, BoosterLastCraft? lastCraft) {
			Bot = bot;
			GameID = gameID;
			Type = type;
			Info = info;
			InitTime = DateTime.Now;
			BoosterQueue = boosterQueue;
			LastCraft = lastCraft;
		}

		internal async Task<Steam.BoostersResponse?> Craft(TradabilityPreference nTp) {
			Steam.BoostersResponse? result = await WebRequest.CreateBooster(Bot, Info.AppID, Info.Series, nTp).ConfigureAwait(false);

			if (result?.Result?.Result == EResult.OK) {
				SetWasCrafted();
			}

			return result;
		}

		internal void SetWasCrafted() {
			BoosterQueue.UpdateLastCraft(GameID, DateTime.Now);
			WasCrafted = true;
		}

		internal DateTime GetAvailableAtTime(int delayInSeconds = 0) {
			if (Info.Unavailable && Info.AvailableAtTime != null) {
				if (LastCraft != null) {
					// If this booster had a delay the last time it was crafted then, because of the 24 hour 
					// cooldown, that delay still exists, and doesn't need to be added in again.  If the new delay 
					// is bigger then the old one, then we'll still need to delay some more.
					delayInSeconds = Math.Max(0, delayInSeconds - LastCraft.BoosterDelay);
				}

				if (LastCraft == null || LastCraft.CraftTime.AddDays(1) > Info.AvailableAtTime.Value.AddMinutes(1)) {
					// Unavailable boosters become available exactly 24 hours after being crafted, down to the second, but Steam 
					// doesn't tell us which second that is.  To get around this, we try to save the exact craft time.  If that 
					// fails, then we use Steam's time and round up a minute get a time we know the booster will be available at.
					return Info.AvailableAtTime.Value.AddMinutes(1).AddSeconds(delayInSeconds);
				}

				return LastCraft.CraftTime.AddDays(1).AddSeconds(delayInSeconds);
			}

			return InitTime.AddSeconds(delayInSeconds);
		}
	}
}
