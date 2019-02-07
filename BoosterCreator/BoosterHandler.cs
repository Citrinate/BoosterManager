using ArchiSteamFarm;
using ArchiSteamFarm.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
		private readonly IReadOnlyCollection<uint> GameIDs;
		private readonly Timer BoosterTimer;

		internal static ConcurrentDictionary<string, BoosterHandler> BoosterHandlers = new ConcurrentDictionary<string, BoosterHandler>();

		internal BoosterHandler([NotNull] Bot bot, IReadOnlyCollection<uint> gameIDs) {
			Bot = bot ?? throw new ArgumentNullException(nameof(bot));
			GameIDs = gameIDs;

			BoosterTimer = new Timer(
				async e => await AutoBooster().ConfigureAwait(false),
				null,
				TimeSpan.FromHours(1.1) + TimeSpan.FromSeconds(Bot.BotsReadOnly.Count * ASF.GlobalConfig.LoginLimiterDelay * 3),
				TimeSpan.FromHours(8.1)
			);
		}

		public void Dispose() => BoosterTimer.Dispose();

		private async Task AutoBooster() {
			if (!Bot.IsConnectedAndLoggedOn) {
				return;
			}

			await CreateBooster(Bot, GameIDs).ConfigureAwait(false);
		}

		internal static async Task<string> CreateBooster(Bot bot, IReadOnlyCollection<uint> gameIDs) {
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

			foreach (uint gameID in gameIDs) {
				await Task.Delay(500).ConfigureAwait(false);

				if (!boosterInfos.ContainsKey(gameID)) {
					response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicense, gameID, "NotEligible")));

					continue;
				}

				Steam.BoosterInfo bi = boosterInfos[gameID];

				if (gooAmount < bi.Price) {
					response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicense, gameID, "NotEnoughGems")));

					continue;
				}

				if (bi.Unavailable) {
					response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicense, gameID, $"Available at time: {bi.AvailableAtTime}")));

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

					continue;
				}
				
				gooAmount = result.GooAmount;
				tradableGooAmount = result.TradableGooAmount;
				unTradableGooAmount = result.UntradableGooAmount;
				response.AppendLine(Commands.FormatBotResponse(bot, string.Format(Strings.BotAddLicenseWithItems, bi.AppID, EResult.OK, bi.Name)));
			}

			return response.Length > 0 ? response.ToString() : null;
		}
	}
}
