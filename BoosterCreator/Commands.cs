using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Storage;
using ArchiSteamFarm.Localization;

namespace BoosterCreator {
	internal static class Commands {
		internal static async Task<string?> Response(Bot bot, ulong steamID, string message, string[] args) {
			switch (args[0].ToUpperInvariant()) {
				case "BOOSTER" when args.Length > 2:
					return await ResponseBooster(steamID, args[1], args[2]).ConfigureAwait(false);
				case "BOOSTER":
					return await ResponseBooster(bot, steamID, args[1]).ConfigureAwait(false);
				default:
					return null;
			}
		}

		private static async Task<string?> ResponseBooster(Bot bot, ulong steamID, string targetGameIDs) {
			if ((steamID == 0) || string.IsNullOrEmpty(targetGameIDs)) {
				ASF.ArchiLogger.LogNullError(nameof(steamID) + " || " + nameof(targetGameIDs));

				return null;
			}

			if (!bot.HasAccess(steamID, BotConfig.EAccess.Operator)) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			string[] gameIDs = targetGameIDs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (gameIDs.Length == 0) {
				return FormatBotResponse(bot, string.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
			}


			ConcurrentDictionary<uint, DateTime?> gamesToBooster = new ConcurrentDictionary<uint, DateTime?>();
			//HashSet<uint> gamesToBooster = new HashSet<uint>();

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return FormatBotResponse(bot, string.Format(Strings.ErrorParsingObject, nameof(gameID)));
				}

				gamesToBooster.TryAdd(gameID, null);
			}

			return await BoosterHandler.CreateBooster(bot, gamesToBooster).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseBooster(ulong steamID, string botNames, string targetGameIDs) {
			if ((steamID == 0) || string.IsNullOrEmpty(botNames) || string.IsNullOrEmpty(targetGameIDs)) {
				ASF.ArchiLogger.LogNullError(nameof(steamID) + " || " + nameof(botNames) + " || " + nameof(targetGameIDs));

				return null;
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return ASF.IsOwner(steamID) ? FormatStaticResponse(string.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseBooster(bot, steamID, targetGameIDs))).ConfigureAwait(false);

			List<string?> responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		internal static string FormatStaticResponse(string response) => Commands.FormatStaticResponse(response);
		internal static string FormatBotResponse(Bot bot, string response) => bot.Commands.FormatBotResponse(response);
	}
}
