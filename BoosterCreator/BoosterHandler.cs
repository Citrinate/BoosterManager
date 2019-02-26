using ArchiSteamFarm;
using ArchiSteamFarm.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SteamKit2;
using Newtonsoft.Json;
using JetBrains.Annotations;

namespace BoosterCreator {
	internal sealed class BoosterHandler : IDisposable {
		private readonly Bot Bot;
		private readonly ConcurrentDictionary<uint,DateTime?> GameIDs = new ConcurrentDictionary<uint, DateTime?>();
		private readonly Timer BoosterTimer;

		internal static ConcurrentDictionary<string, BoosterHandler> BoosterHandlers = new ConcurrentDictionary<string, BoosterHandler>();

		internal BoosterHandler([NotNull] Bot bot, IReadOnlyCollection<uint> gameIDs) {
			Bot = bot ?? throw new ArgumentNullException(nameof(bot));
			foreach(var gameID in gameIDs) {
				GameIDs.TryAdd(gameID, DateTime.Now.AddHours(1));
			}

			BoosterTimer = new Timer(
				async e => await AutoBooster().ConfigureAwait(false),
				null,
				TimeSpan.FromHours(1) + TimeSpan.FromSeconds(Bot.BotsReadOnly.Count * ASF.GlobalConfig.LoginLimiterDelay * 3),
				TimeSpan.FromHours(1)
			);
		}

		public void Dispose() => BoosterTimer.Dispose();

		private async Task AutoBooster() {
			if (!Bot.IsConnectedAndLoggedOn) {
				return;
			}

			string response = await CreateBooster(Bot, GameIDs).ConfigureAwait(false);
			ASF.ArchiLogger.LogGenericInfo (response);
		}

		internal static async Task<string> CreateBooster(Bot bot, ConcurrentDictionary<uint, DateTime?> gameIDs) {
			if (!gameIDs.Any()) {
				bot.ArchiLogger.LogNullError(nameof(gameIDs));

				return null;
			}

			HtmlDocument boosterPage = await WebRequest.GetBoosterPage(bot).ConfigureAwait(false);

			if (boosterPage == null) {
				bot.ArchiLogger.LogNullError(nameof(boosterPage));

				return Strings.WarningFailed;
			}

			MatchCollection gooAmounts = Regex.Matches(boosterPage.Text, "(?<=parseFloat\\( \")[0-9]+");
			Match info = Regex.Match(boosterPage.Text, "\\[\\{\"[\\s\\S]*\"}]");

			if (!info.Success || (gooAmounts.Count != 3)) {
				return Strings.WarningFailed;
			}

			uint gooAmount = uint.Parse(gooAmounts[0].Value);
			uint tradableGooAmount = uint.Parse(gooAmounts[1].Value);
			uint unTradableGooAmount = uint.Parse(gooAmounts[2].Value);

			Dictionary<uint, Steam.BoosterInfo> boosterInfos = JsonConvert.DeserializeObject<IEnumerable<Steam.BoosterInfo>>(info.Value).ToDictionary(boosterInfo => boosterInfo.AppID);

			StringBuilder response = new StringBuilder();

			foreach (var gameID in gameIDs) {
				await Task.Delay(500).ConfigureAwait(false);

				if (!boosterInfos.ContainsKey(gameID.Key)) {
					response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicense, gameID, "NotEligible")));
					//If we are not eligible - wait 8 hours, just in case game will be added to account later
					if (gameID.Value.HasValue) { //if source is timer, not command
						gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
					}
					continue;
				}

				Steam.BoosterInfo bi = boosterInfos[gameID.Key];

				if (gooAmount < bi.Price) {
					response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicense, gameID, "NotEnoughGems")));
					//If we have not enough gems - wait 8 hours, just in case gems will be added to account later
					if (gameID.Value.HasValue) { //if source is timer, not command
						gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
					}
					continue;
				}

				if (bi.Unavailable) {
					response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicense, gameID, $"Available at time: {bi.AvailableAtTime}")));
					//Wait until specified time
					DateTime availableAtTime;

					if (DateTime.TryParseExact(bi.AvailableAtTime, "d MMM @ h:mmtt", CultureInfo.CurrentCulture, DateTimeStyles.None, out availableAtTime)) {
						DateTime convertedTime = TimeZoneInfo.ConvertTime(availableAtTime, ValveTimeZone.GetTimeZoneInfo(), TimeZoneInfo.Local);
						if (gameID.Value.HasValue) { //if source is timer, not command
							gameIDs[gameID.Key] = convertedTime;
						}
					} else {
						ASF.ArchiLogger.LogGenericInfo("Unable to parse time \""+ bi.AvailableAtTime+"\", please report this.");
					}
					continue;
				}

				uint nTp;

				if (unTradableGooAmount > 0) {
					nTp = tradableGooAmount > bi.Price ? (uint) 1 : 3;
				} else {
					nTp = 2;
				}

				Steam.BoostersResponse result = await WebRequest.CreateBooster(bot, bi.AppID, bi.Series, nTp).ConfigureAwait(false);

				if (result?.Result?.Result != EResult.OK) {
					response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicense, bi.AppID, EResult.Fail)));
					//Some unhandled error - wait 8 hours before retry
					if (gameID.Value.HasValue) { //if source is timer, not command
						gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
					}
					continue;
				}
				
				gooAmount = result.GooAmount;
				tradableGooAmount = result.TradableGooAmount;
				unTradableGooAmount = result.UntradableGooAmount;
				response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicenseWithItems, bi.AppID, EResult.OK, bi.Name)));
				//Buster was made - next is only available in 24 hours
				if (gameID.Value.HasValue) { //if source is timer, not command
					gameIDs[gameID.Key] = DateTime.Now.AddHours(24);
				}


			}

			//Get nearest time when we should try for new booster;
			DateTime? nextTry = gameIDs.Values.Min<DateTime?>();

			if (nextTry.HasValue) { //if it was not from command
				//Add 10 minutes to avoid race conditions
				BoosterHandler.BoosterHandlers[bot.BotName].BoosterTimer.Change(nextTry.Value - DateTime.Now + TimeSpan.FromMinutes(10), nextTry.Value - DateTime.Now + TimeSpan.FromMinutes(10));
			}

			return response.Length > 0 ? response.ToString() : null;
		}
	}
}
