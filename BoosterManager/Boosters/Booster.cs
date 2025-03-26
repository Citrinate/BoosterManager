using ArchiSteamFarm.Steam;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoosterManager {
	internal sealed class Booster {
		private readonly Bot Bot;
		private BotCache BotCache => BoosterHandler.BoosterHandlers[Bot.BotName].BotCache;
		internal readonly BoosterJob BoosterJob;
		internal readonly uint GameID;
		internal readonly Steam.BoosterInfo Info;
		private readonly DateTime InitTime;
		private readonly BoosterLastCraft? LastCraft;
		internal bool WasCrafted { get; private set; } = false;

		internal Booster(Bot bot, uint gameID, Steam.BoosterInfo info, BoosterJob boosterJob) {
			Bot = bot;
			GameID = gameID;
			Info = info;
			InitTime = DateTime.Now;
			BoosterJob = boosterJob;
			LastCraft = BotCache.GetLastCraft(gameID);
		}

		internal async Task<Steam.BoostersResponse?> Craft(Steam.TradabilityPreference nTp) {
			await BotCache.PreCraft(this).ConfigureAwait(false);

			Steam.BoostersResponse? result = await WebRequest.CreateBooster(Bot, Info.AppID, Info.Series, nTp).ConfigureAwait(false);

			if (result?.Result?.Result == EResult.OK) {
				SetWasCrafted();
			}

			return result;
		}

		internal void SetWasCrafted() {
			BotCache.PostCraft();
			BotCache.SetLastCraft(GameID, DateTime.Now);
			WasCrafted = true;
		}

		internal DateTime GetAvailableAtTime() {
			if (Info.Unavailable && Info.AvailableAtTime != null) {
				if (LastCraft == null 
					|| LastCraft.CraftTime.AddDays(1) > Info.AvailableAtTime.Value.AddMinutes(1)
					|| (Info.AvailableAtTime.Value.AddMinutes(1) - LastCraft.CraftTime.AddDays(1)).TotalMinutes > 2 // LastCraft time is too old to be used
				) {
					// Unavailable boosters become available exactly 24 hours after being crafted, down to the second, but Steam 
					// doesn't tell us which second that is.  To get around this, we try to save the exact craft time.  If that 
					// fails, then we use Steam's time and round up a minute get a time we know the booster will be available at.
					return Info.AvailableAtTime.Value.AddMinutes(1);
				}

				return LastCraft.CraftTime.AddDays(1);
			}

			return InitTime;
		}
	}

	internal class BoosterComparer : IEqualityComparer<Booster> {
		public bool Equals(Booster? x, Booster? y) {
			return x?.GameID == y?.GameID;
		}

		public int GetHashCode(Booster obj) {
			return HashCode.Combine(obj.GameID);
		}
	}
}
