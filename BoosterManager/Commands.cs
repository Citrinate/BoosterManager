using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Localization;

namespace BoosterManager {
	internal static class Commands {
		internal static async Task<string?> Response(Bot bot, EAccess access, ulong steamID, string message, string[] args) {
			if (string.IsNullOrEmpty(message)) {
				return null;
			}

			// TODO:
			// "LOOTBOOSTERS"
			// "TRANSFERBOOSTERS"
			// "LOOTGEMS"
			// "LOOTSACKS"
			// "KEYS"
			// "LOOTKEYS"
			// "TRANSFERKEYS"
			// "LOGDATA"
			switch (args.Length) {
				case 1:
					switch (args[0].ToUpperInvariant()) {
						case "BSTATUS":
							return ResponseBoosterStatus(bot, access);
						case "BSTOPALL":
							return ResponseBoosterStopTime(bot, access, "0");
						case "GEMS":
							return await ResponseGems(bot, access).ConfigureAwait(false);
						default:
							return null;
					};
				default:
					switch (args[0].ToUpperInvariant()) {
						case "BOOSTER" when args.Length > 2:
							return ResponseBooster(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ","), bot);
						case "BOOSTER":
							return ResponseBooster(bot, access, steamID, args[1]);
						case "BSTATUS":
							return ResponseBoosterStatus(access, steamID, args[1]);
						case "BSTOP" when args.Length > 2:
							return ResponseBoosterStop(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ","));
						case "BSTOP":
							return ResponseBoosterStop(bot, access, args[1]);
						case "BSTOPALL":
							return ResponseBoosterStopTime(access, steamID, args[1], "0");
						case "BSTOPTIME" when args.Length > 2:
							return ResponseBoosterStopTime(access, steamID, args[1], args[2]);
						case "BSTOPTIME":
							return ResponseBoosterStopTime(bot, access, args[1]);
						case "GEMS":
							return await ResponseGems(access, steamID, args[1]).ConfigureAwait(false);
						case "TRANSFERGEMS" when args.Length > 3:
							return await ResponseTransferGems(access, steamID, args[1], args[2], args[3]).ConfigureAwait(false);
						case "TRANSFERGEMS" when args.Length > 2:
							return await ResponseTransferGems(bot, access, args[1], args[2]).ConfigureAwait(false);
						default:
							return null;
					};				
			}
		}

		private static string? ResponseBooster(Bot bot, EAccess access, ulong steamID, string targetGameIDs, Bot? respondingBot = null) {
			if (String.IsNullOrEmpty(targetGameIDs)) {
				throw new ArgumentNullException(nameof(targetGameIDs));
			}

			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			string[] gameIDs = targetGameIDs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (gameIDs.Length == 0) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
			}

			HashSet<uint> gamesToBooster = new HashSet<uint>();

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return FormatBotResponse(bot, String.Format(Strings.ErrorParsingObject, nameof(gameID)));
				}

				gamesToBooster.Add(gameID);
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].ScheduleBoosters(gamesToBooster, respondingBot ?? bot, steamID);
		}

		private static string? ResponseBooster(EAccess access, ulong steamID, string botNames, string targetGameIDs, Bot respondingBot) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			if (String.IsNullOrEmpty(targetGameIDs)) {
				throw new ArgumentNullException(nameof(targetGameIDs));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot =>  ResponseBooster(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), steamID, targetGameIDs, respondingBot));

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseBoosterStatus(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].GetStatus();
		}

		private static string? ResponseBoosterStatus(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot => ResponseBoosterStatus(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)));

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseBoosterStop(Bot bot, EAccess access, string targetGameIDs) {
			if (String.IsNullOrEmpty(targetGameIDs)) {
				throw new ArgumentNullException(nameof(targetGameIDs));
			}

			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			string[] gameIDs = targetGameIDs.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			if (gameIDs.Length == 0) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
			}

			HashSet<uint> gamesToStop = new HashSet<uint>();

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return FormatBotResponse(bot, String.Format(Strings.ErrorParsingObject, nameof(gameID)));
				}

				gamesToStop.Add(gameID);
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].UnscheduleBoosters(gameIDs: gamesToStop);
		}

		private static string? ResponseBoosterStop(EAccess access, ulong steamID, string botNames, string targetGameIDs) {
			if (String.IsNullOrEmpty(targetGameIDs)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			if (String.IsNullOrEmpty(targetGameIDs)) {
				throw new ArgumentNullException(nameof(targetGameIDs));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot => ResponseBoosterStop(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), targetGameIDs));

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseBoosterStopTime(Bot bot, EAccess access, string timeLimit) {
			if (String.IsNullOrEmpty(timeLimit)) {
				throw new ArgumentNullException(nameof(timeLimit));
			}

			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			if (!uint.TryParse(timeLimit, out uint timeLimitHours)) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsInvalid, nameof(timeLimit)));
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].UnscheduleBoosters(timeLimitHours: (int) timeLimitHours);
		}

		private static string? ResponseBoosterStopTime(EAccess access, ulong steamID, string botNames, string timeLimit) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot => ResponseBoosterStopTime(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), timeLimit));

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseGems(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return await GemHandler.GetGemCount(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseGems(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseGems(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseTransferGems(Bot bot, EAccess access, string botNames, string gemAmounts) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			if (String.IsNullOrEmpty(gemAmounts)) {
				throw new ArgumentNullException(nameof(gemAmounts));
			}

			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			bots.Remove(bot);
			string[] amounts = gemAmounts.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);			
			if (amounts.Length == 1 && bots.Count > 1) {
				amounts = Enumerable.Repeat(amounts[0], bots.Count).ToArray();
			}

			if (amounts.Length == 0) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsEmpty, nameof(amounts)));
			}
			
			if (amounts.Length != bots.Count) {
				return FormatBotResponse(bot, String.Format("Number of recieving bots ({0}) does not match number of gem amounts ({1})", bots.Count, amounts.Length));
			}

			List<uint> amountNums = new List<uint>();
			foreach (string amount in amounts) {
				if (!uint.TryParse(amount, out uint amountNum) || (amountNum == 0)) {
					return FormatBotResponse(bot, String.Format(Strings.ErrorParsingObject, nameof(amountNum)));
				}

				amountNums.Add(amountNum);
			}

			List<(Bot reciever, uint amount)> recievers = bots.Zip(amountNums).Select(pair => (pair.First, pair.Second)).ToList();

			return await GemHandler.TransferGems(bot, recievers).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseTransferGems(EAccess access, ulong steamID, string senderBotName, string botNames, string gemAmounts) {
			if (String.IsNullOrEmpty(senderBotName)) {
				throw new ArgumentNullException(nameof(senderBotName));
			}

			HashSet<Bot>? senders = Bot.GetBots(senderBotName);

			if ((senders == null) || (senders.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, senderBotName)) : null;
			}
			
			if (senders.Count > 1) {
				return FormatStaticResponse("Can't have more than 1 sender");
			}

			Bot sender = senders.First();

			return await ResponseTransferGems(sender, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(sender, access, steamID), botNames, gemAmounts).ConfigureAwait(false);
		}

		internal static string FormatStaticResponse(string response) => ArchiSteamFarm.Steam.Interaction.Commands.FormatStaticResponse(response);
		internal static string FormatBotResponse(Bot bot, string response) => bot.Commands.FormatBotResponse(response);
	}
}
