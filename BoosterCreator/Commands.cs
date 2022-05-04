using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Localization;

namespace BoosterCreator {
	internal static class Commands {
		internal static async Task<string?> Response(Bot bot, EAccess access, ulong steamID, string message, string[] args) {
			if (string.IsNullOrEmpty(message)) {
				return null;
			}
			return args[0].ToUpperInvariant() switch {
				"BOOSTER" when args.Length > 2 => await ResponseBooster(access, steamID, args[1], args[2]).ConfigureAwait(false),
				"BOOSTER" => await ResponseBooster(bot, access, args[1]).ConfigureAwait(false),
				_ => null,
			};
		}

		private static async Task<string?> ResponseBooster(Bot bot, EAccess access, string targetGameIDs) {
			if (string.IsNullOrEmpty(targetGameIDs)) {
				ASF.ArchiLogger.LogNullError(null, nameof(targetGameIDs));

				return null;
			}

			if (access < EAccess.Operator) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			string[] gameIDs = targetGameIDs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (gameIDs.Length == 0) {
				return FormatBotResponse(bot, string.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
			}


			ConcurrentDictionary<uint, DateTime?> gamesToBooster = new();
			//HashSet<uint> gamesToBooster = new HashSet<uint>();

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return FormatBotResponse(bot, string.Format(Strings.ErrorParsingObject, nameof(gameID)));
				}

				gamesToBooster.TryAdd(gameID, null);
			}

			return await BoosterHandler.CreateBooster(bot, gamesToBooster).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseBooster(EAccess access, ulong steamID, string botNames, string targetGameIDs) {
			if (string.IsNullOrEmpty(botNames) || string.IsNullOrEmpty(targetGameIDs)) {
				ASF.ArchiLogger.LogNullError(null, nameof(botNames) + " || " + nameof(targetGameIDs));

				return null;
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(string.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseBooster(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), targetGameIDs))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		internal static string FormatStaticResponse(string response) => Commands.FormatStaticResponse(response);
		internal static string FormatBotResponse(Bot bot, string response) => bot.Commands.FormatBotResponse(response);
	}
}
