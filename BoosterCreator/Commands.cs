using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Localization;

namespace BoosterCreator {
	internal static class Commands {
#pragma warning disable 1998
		internal static async Task<string?> Response(Bot bot, EAccess access, ulong steamID, string message, string[] args) {
			if (string.IsNullOrEmpty(message)) {
				return null;
			}
			return args[0].ToUpperInvariant() switch {
				"BOOSTER" when args.Length > 2 => ResponseBooster(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ","), bot),
				"BOOSTER" => ResponseBooster(bot, access, steamID, args[1]),
				"BSTATUS" when args.Length > 1 => ResponseBoosterStatus(access, steamID, args[1]),
				"BSTATUS" => ResponseBoosterStatus(bot, access),
				"BSTOP" when args.Length > 2 => ResponseBoosterStop(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ",")),
				"BSTOP" => ResponseBoosterStop(bot, access, args[1]),
				"BSTOPTIME" when args.Length > 2 => ResponseBoosterStopTime(access, steamID, args[1], args[2]),
				"BSTOPTIME" => ResponseBoosterStopTime(bot, access, args[1]),
				"BSTOPALL" when args.Length > 1 => ResponseBoosterStopTime(access, steamID, args[1], "0"),
				"BSTOPALL" => ResponseBoosterStopTime(bot, access, "0"),
				// "GEMS" => TODO,
				// "GTRANSFER" => TODO,
				// "GLOOT" => TODO,
				// "KEYS" => TODO,
				// "KTRANSFER" => TODO,
				// "KLOOT" => TODO,
				// "LOGDATA" => TODO,
				_ => null,
			};
		}
#pragma warning restore 1998

		private static string? ResponseBooster(Bot bot, EAccess access, ulong steamID, string targetGameIDs, Bot? respondingBot = null) {
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

			HashSet<uint> gamesToBooster = new HashSet<uint>();

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return FormatBotResponse(bot, string.Format(Strings.ErrorParsingObject, nameof(gameID)));
				}

				gamesToBooster.Add(gameID);
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].ScheduleBoosters(gamesToBooster, respondingBot ?? bot, steamID);
		}

		private static string? ResponseBooster(EAccess access, ulong steamID, string botNames, string targetGameIDs, Bot respondingBot) {
			if (string.IsNullOrEmpty(botNames) || string.IsNullOrEmpty(targetGameIDs)) {
				ASF.ArchiLogger.LogNullError(null, nameof(botNames) + " || " + nameof(targetGameIDs));

				return null;
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(string.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = 
				bots.Select(
					bot =>  ResponseBooster(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), steamID, targetGameIDs, respondingBot)
				);

			List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseBoosterStatus(Bot bot, EAccess access) {
			if (access < EAccess.Operator) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].GetStatus();
		}

		private static string? ResponseBoosterStatus(EAccess access, ulong steamID, string botNames) {
			if (string.IsNullOrEmpty(botNames)) {
				ASF.ArchiLogger.LogNullError(null, nameof(botNames));

				return null;
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(string.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = 
				bots.Select(
					bot => ResponseBoosterStatus(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID))
				);

			List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseBoosterStop(Bot bot, EAccess access, string targetGameIDs) {
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

			HashSet<uint> gamesToStop = new HashSet<uint>();

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return FormatBotResponse(bot, string.Format(Strings.ErrorParsingObject, nameof(gameID)));
				}

				gamesToStop.Add(gameID);
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].UnscheduleBoosters(gameIDs: gamesToStop);
		}

		private static string? ResponseBoosterStop(EAccess access, ulong steamID, string botNames, string targetGameIDs) {
			if (string.IsNullOrEmpty(botNames) || string.IsNullOrEmpty(targetGameIDs)) {
				ASF.ArchiLogger.LogNullError(null, nameof(botNames) + " || " + nameof(targetGameIDs));

				return null;
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(string.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = 
				bots.Select(
					bot => ResponseBoosterStop(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), targetGameIDs)
				);

			List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseBoosterStopTime(Bot bot, EAccess access, string timeLimit) {
			if (access < EAccess.Operator) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			if (!uint.TryParse(timeLimit, out uint timeLimitHours)) {
				return FormatBotResponse(bot, string.Format(Strings.ErrorIsInvalid, nameof(timeLimit)));
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].UnscheduleBoosters(timeLimitHours: (int) timeLimitHours);
		}

		private static string? ResponseBoosterStopTime(EAccess access, ulong steamID, string botNames, string timeLimit) {
			if (string.IsNullOrEmpty(botNames)) {
				ASF.ArchiLogger.LogNullError(null, nameof(botNames));

				return null;
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(string.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = 
				bots.Select(
					bot => ResponseBoosterStopTime(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), timeLimit)
				);

			List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		internal static string FormatStaticResponse(string response) => ArchiSteamFarm.Steam.Interaction.Commands.FormatStaticResponse(response);
		internal static string FormatBotResponse(Bot bot, string response) => bot.Commands.FormatBotResponse(response);
	}
}
