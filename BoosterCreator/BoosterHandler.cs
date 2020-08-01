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
using SteamKit2;
using Newtonsoft.Json;
using JetBrains.Annotations;
using AngleSharp.Dom;

namespace BoosterCreator {
	internal sealed class BoosterHandler : IDisposable {
		private readonly Bot Bot;
		private readonly ConcurrentDictionary<uint, DateTime?> GameIDs = new ConcurrentDictionary<uint, DateTime?>();
		private readonly Timer BoosterTimer;

		internal static ConcurrentDictionary<string, BoosterHandler> BoosterHandlers = new ConcurrentDictionary<string, BoosterHandler>();

		internal const int DelayBetweenBots = 5; //5 minutes between bots

		internal static int GetBotIndex(Bot bot) {
			//this can be pretty slow and memory-consuming on lage bot farm. Luckily, I don't care about cases with >10 bots
			List<string> botnames = BoosterHandlers.Keys.ToList<string>();
			botnames.Sort();
			int index = botnames.IndexOf(bot.BotName);
			return 1 + (index >= 0 ? index : botnames.Count);
		}

		internal BoosterHandler([NotNull] Bot bot, IReadOnlyCollection<uint> gameIDs) {
			Bot = bot ?? throw new ArgumentNullException(nameof(bot));
			foreach (uint gameID in gameIDs) {
				GameIDs.TryAdd(gameID, DateTime.Now.AddMinutes(GetBotIndex(bot) * DelayBetweenBots));
				ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Auto-attempt to make booster from " + gameID.ToString() + " is planned at " + GameIDs[gameID].Value.ToShortDateString() + " " + GameIDs[gameID].Value.ToShortTimeString()));
			}

			BoosterTimer = new Timer(
				async e => await AutoBooster().ConfigureAwait(false),
				null,
				TimeSpan.FromMinutes(GetBotIndex(bot) * DelayBetweenBots),
				TimeSpan.FromMinutes(GetBotIndex(bot) * DelayBetweenBots)
			);
		}

		public void Dispose() => BoosterTimer.Dispose();

		private async Task AutoBooster() {
			if (!Bot.IsConnectedAndLoggedOn) {
				return;
			}

			await CreateBooster(Bot, GameIDs).ConfigureAwait(false);
		}

		internal static async Task<string> CreateBooster(Bot bot, ConcurrentDictionary<uint, DateTime?> gameIDs) {
			if (!gameIDs.Any()) {
				bot.ArchiLogger.LogNullError(nameof(gameIDs));

				return null;
			}
			IDocument boosterPage = await WebRequest.GetBoosterPage(bot).ConfigureAwait(false);
			if (boosterPage == null) {
				bot.ArchiLogger.LogNullError(nameof(boosterPage));

				return Commands.FormatBotResponse(bot, string.Format(Strings.ErrorFailingRequest, boosterPage));
				;
			}
			MatchCollection gooAmounts = Regex.Matches(boosterPage.Source.Text, "(?<=parseFloat\\( \")[0-9]+");
			Match info = Regex.Match(boosterPage.Source.Text, "\\[\\{\"[\\s\\S]*\"}]");
			if (!info.Success || (gooAmounts.Count != 3)) {
				bot.ArchiLogger.LogGenericError(string.Format(Strings.ErrorParsingObject, boosterPage));
				return Commands.FormatBotResponse(bot, string.Format(Strings.ErrorParsingObject, boosterPage));
			}
			uint gooAmount = uint.Parse(gooAmounts[0].Value);
			uint tradableGooAmount = uint.Parse(gooAmounts[1].Value);
			uint unTradableGooAmount = uint.Parse(gooAmounts[2].Value);

			Dictionary<uint, Steam.BoosterInfo> boosterInfos = JsonConvert.DeserializeObject<IEnumerable<Steam.BoosterInfo>>(info.Value).ToDictionary(boosterInfo => boosterInfo.AppID);
			StringBuilder response = new StringBuilder();

			foreach (KeyValuePair<uint, DateTime?> gameID in gameIDs) {
				if (!gameID.Value.HasValue || DateTime.Compare(gameID.Value.Value, DateTime.Now) <= 0) {
					await Task.Delay(500).ConfigureAwait(false);
					if (!boosterInfos.ContainsKey(gameID.Key)) {
						response.AppendLine(Commands.FormatBotResponse(bot, "Not eligible to create boosters from " + gameID.Key.ToString()));
						ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Not eligible to create boosters from " + gameID.Key.ToString()));
						//If we are not eligible - wait 8 hours, just in case game will be added to account later
						if (gameID.Value.HasValue) { //if source is timer, not command
							gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
							ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key.ToString() + " is planned at " + gameIDs[gameID.Key].Value.ToShortDateString() + " " + gameIDs[gameID.Key].Value.ToShortTimeString()));
						}
						continue;
					}
					Steam.BoosterInfo bi = boosterInfos[gameID.Key];
					if (gooAmount < bi.Price) {
						response.AppendLine(Commands.FormatBotResponse(bot, "Not enough gems to create booster from " + gameID.Key.ToString()));
						//If we have not enough gems - wait 8 hours, just in case gems will be added to account later
						ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Not enough gems to create booster from " + gameID.Key.ToString()));
						if (gameID.Value.HasValue) { //if source is timer, not command
							gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
							ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key.ToString() + " is planned at " + gameIDs[gameID.Key].Value.ToShortDateString() + " " + gameIDs[gameID.Key].Value.ToShortTimeString()));
						}
						continue;
					}

					if (bi.Unavailable) {

						//God, I hate this shit. But for now I have no idea how to predict/enforce correct format.
						string timeFormat;
						if (!string.IsNullOrWhiteSpace(bi.AvailableAtTime) && char.IsDigit(bi.AvailableAtTime.Trim()[0])) {
							timeFormat = "d MMM @ h:mmtt";
						} else {
							timeFormat = "MMM d @ h:mmtt";
						}

						response.AppendLine(Commands.FormatBotResponse(bot, "Crafting booster from " + gameID.Key.ToString() + " will be available at time: " + bi.AvailableAtTime));
						ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Crafting booster from " + gameID.Key.ToString() + " is not availiable now"));
						//Wait until specified time
						if (DateTime.TryParseExact(bi.AvailableAtTime, timeFormat, new CultureInfo("en-US"), DateTimeStyles.None, out DateTime availableAtTime)) {
						} else {
							ASF.ArchiLogger.LogGenericInfo("Unable to parse time \"" + bi.AvailableAtTime + "\", please report this.");
							availableAtTime = DateTime.Now.AddHours(8); //fallback to 8 hours in case of error
						}
						if (gameID.Value.HasValue) { //if source is timer, not command
							gameIDs[gameID.Key] = availableAtTime;//convertedTime;
							ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key.ToString() + " is planned at " + gameIDs[gameID.Key].Value.ToShortDateString() + " " + gameIDs[gameID.Key].Value.ToShortTimeString()));
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
						response.AppendLine(Commands.FormatBotResponse(bot, "Failed to create booster from " + gameID.Key.ToString()));
						ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Failed to create booster from " + gameID.Key.ToString()));
						//Some unhandled error - wait 8 hours before retry
						if (gameID.Value.HasValue) { //if source is timer, not command
							gameIDs[gameID.Key] = DateTime.Now.AddHours(8);
							ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key.ToString() + " is planned at " + gameIDs[gameID.Key].Value.ToShortDateString() + " " + gameIDs[gameID.Key].Value.ToShortTimeString()));
						}
						continue;
					}
					gooAmount = result.GooAmount;
					tradableGooAmount = result.TradableGooAmount;
					unTradableGooAmount = result.UntradableGooAmount;
					response.AppendLine(Commands.FormatBotResponse(bot, "Successfuly created booster from " + gameID.Key.ToString()));
					ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Successfuly created booster from " + gameID.Key.ToString()));
					//Buster was made - next is only available in 24 hours
					if (gameID.Value.HasValue) { //if source is timer, not command
						gameIDs[gameID.Key] = DateTime.Now.AddHours(24);
						ASF.ArchiLogger.LogGenericInfo(Commands.FormatBotResponse(bot, "Next attempt to make booster from " + gameID.Key.ToString() + " is planned at " + gameIDs[gameID.Key].Value.ToShortDateString() + " " + gameIDs[gameID.Key].Value.ToShortTimeString()));
					}
				}

			}
			//Get nearest time when we should try for new booster;
			DateTime? nextTry = gameIDs.Values.Min<DateTime?>();
			if (nextTry.HasValue) { //if it was not from command
				BoosterHandler.BoosterHandlers[bot.BotName].BoosterTimer.Change(nextTry.Value - DateTime.Now + TimeSpan.FromMinutes(GetBotIndex(bot) * DelayBetweenBots), nextTry.Value - DateTime.Now + TimeSpan.FromMinutes(GetBotIndex(bot) * DelayBetweenBots));
			}
			return response.Length > 0 ? response.ToString() : null;
		}
	}
}
