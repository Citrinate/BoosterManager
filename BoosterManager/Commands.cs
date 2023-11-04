using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam.Data;
using ArchiSteamFarm.Steam.Security;
using System.ComponentModel;

namespace BoosterManager {
	internal static class Commands {
		internal static async Task<string?> Response(Bot bot, EAccess access, ulong steamID, string message, string[] args) {
			if (!Enum.IsDefined(access)) {
				throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
			}

			if (string.IsNullOrEmpty(message)) {
				return null;
			}

			switch (args.Length) {
				case 1:
					switch (args[0].ToUpperInvariant()) {
						case "BSA":
							return ResponseBoosterStatus(access, steamID, "ASF");
						case "BSTATUS" or "BOOSTERSTATUS":
							return ResponseBoosterStatus(bot, access);

						case "BSA^":
							return ResponseBoosterStatus(access, steamID, "ASF", true);
						case "BSTATUS^" or "BOOSTERSTATUS^":
							return ResponseBoosterStatus(bot, access, true);
						
						case "BSTOPALL" or "BOOSTERSTOPALL":
							return ResponseBoosterStopTime(bot, access, "0");
						
						case "GA":
							return await ResponseGems(access, steamID, "ASF").ConfigureAwait(false);
						case "GEMS" or "GEM":
							return await ResponseGems(bot, access).ConfigureAwait(false);
						
						case "KA":
							return await ResponseKeys(access, steamID, "ASF").ConfigureAwait(false);
						case "KEYS" or "KEY":
							return await ResponseKeys(bot, access).ConfigureAwait(false);
						
						case "LIA":
							return await ResponseListings(access, steamID, "ASF").ConfigureAwait(false);
						case "LISTINGS" or "LISTING":
							return await ResponseListings(bot, access).ConfigureAwait(false);
						
						case "LOGBOOSTERDATA" or "SENDBOOSTERDATA" or "LOGBOOSTERS" or "SENDBOOSTERS" or "LOGBD" or "SENDBD" or "LOGB" or "SENDB":
							return await ResponseLogBoosterData(bot, access).ConfigureAwait(false);

						case "LDA" or "LOGA":
							return await ResponseLogData(access, steamID, "ASF").ConfigureAwait(false);
						case "LOGDATA" or "SENDDATA":
							return await ResponseLogData(bot, access).ConfigureAwait(false);

						case "LOGSTOP" or "STOPLOG" or "LOGDATASTOP" or "SENDDATASTOP" or "STOPLOGDATA" or "STOPSENDDATA":
							return ResponseLogStop(bot, access);

						case "LOGINVENTORYHISTORY" or "SENDINVENTORYHISTORY" or "LOGIH" or "SENDIH":
							return await ResponseLogInventoryHistory(bot, access, steamID).ConfigureAwait(false);

						case "LOGMARKETHISTORY" or "SENDMARKETHISTORY" or "LOGMH" or "SENDMH":
							return await ResponseLogMarketHistory(bot, access).ConfigureAwait(false);

						case "LOGMARKETLISTINGS" or "SENDMARKETLISTINGS" or "LOGML" or "SENDML":
							return await ResponseLogMarketListings(bot, access).ConfigureAwait(false);
						
						case "LBA":
							return await ResponseSendItems(access, steamID, "ASF", Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.BoosterPack).ConfigureAwait(false);
						case "LOOTBOOSTERS" or "LOOTBOOSTER":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.BoosterPack).ConfigureAwait(false);
						
						case "LCA":
							return await ResponseSendItems(access, steamID, "ASF", Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.TradingCard).ConfigureAwait(false);
						case "LOOTCARDS" or "LOOTCARD":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.TradingCard).ConfigureAwait(false);
						
						case "LFA":
							return await ResponseSendItems(access, steamID, "ASF", Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard).ConfigureAwait(false);
						case "LOOTFOILS" or "LOOTFOIL":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard).ConfigureAwait(false);
						
						case "LGA":
							return await ResponseSendItems(access, steamID, "ASF", Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.GemsClassID, allowUnmarketable: true).ConfigureAwait(false);
						case "LOOTGEMS" or "LOOTGEM":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.GemsClassID, allowUnmarketable: true).ConfigureAwait(false);
						
						case "LKA":
							return await ResponseSendItems(access, steamID, "ASF", KeyHandler.KeyAppID, KeyHandler.KeyContextID, KeyHandler.KeyType, marketHash: KeyHandler.MarketHash).ConfigureAwait(false);
						case "LOOTKEYS" or "LOOTKEY":
							return await ResponseSendItems(bot, access, KeyHandler.KeyAppID, KeyHandler.KeyContextID, KeyHandler.KeyType, marketHash: KeyHandler.MarketHash).ConfigureAwait(false);
						
						case "LSA":
							return await ResponseSendItems(access, steamID, "ASF", Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.SackOfGemsClassID).ConfigureAwait(false);
						case "LOOTSACKS" or "LOOTSACK":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.SackOfGemsClassID).ConfigureAwait(false);
						
						case "M2FAOKA":
							return await Response2FAOK(access, steamID, "ASF", Confirmation.EConfirmationType.Market).ConfigureAwait(false);
						case "MARKET2FAOK" or "M2FAOK":
							return await Response2FAOK(bot, access, Confirmation.EConfirmationType.Market).ConfigureAwait(false);

						case "T2FAOKA":
							return await Response2FAOK(access, steamID, "ASF", Confirmation.EConfirmationType.Trade).ConfigureAwait(false);
						case "TRADE2FAOK" or "T2FAOK":
							return await Response2FAOK(bot, access, Confirmation.EConfirmationType.Trade).ConfigureAwait(false);

						case "UNPACKGEMS" or "UNPACKGEM":
							return await ResponseUnpackGems(bot, access).ConfigureAwait(false);
						
						case "VA":
							return await ResponseValue(access, steamID, "ASF").ConfigureAwait(false);
						case "VALUE":
							return await ResponseValue(bot, access).ConfigureAwait(false);
						
						default:
							return null;
					};
				default:
					switch (args[0].ToUpperInvariant()) {
						case "BOOSTER" or "BOOSTERS" when args.Length > 2:
							return ResponseBooster(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ","), bot);
						case "BOOSTER" or "BOOSTERS":
							return ResponseBooster(bot, access, steamID, args[1]);
						
						case "BSTATUS" or "BOOSTERSTATUS":
							return ResponseBoosterStatus(access, steamID, args[1]);
						
						case "BSTOP" or "BOOSTERSTOP" when args.Length > 2:
							return ResponseBoosterStop(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ","));
						case "BSTOP" or "BOOSTERSTOP":
							return ResponseBoosterStop(bot, access, args[1]);
						
						case "BSTOPALL" or "BOOSTERSTOPALL":
							return ResponseBoosterStopTime(access, steamID, args[1], "0");
						
						case "BSTOPTIME" or "BOOSTERSTOPTIME" when args.Length > 2:
							return ResponseBoosterStopTime(access, steamID, args[1], args[2]);
						case "BSTOPTIME" or "BOOSTERSTOPTIME":
							return ResponseBoosterStopTime(bot, access, args[1]);
						
						case "FINDLISTINGS" or "FINDLISTING" or "FLISTINGS" or "FLISTING" or "FINDL" when args.Length > 2:
							return await ResponseFindListings(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, " ")).ConfigureAwait(false);
						
						case "FINDANDREMOVELISTINGS" or "FINDANDREMOVELISTING" or "FINDREMOVELISTINGS" or "FINDREMOVELISTING" or "FINDANDCANCELLISTINGS" or "FINDANDCANCELLISTING" or "FINDCANCELLISTINGS" or "FINDCANCELLISTING" or "FRLISTINGS" or "FRLISTING" or "FINDREMOVEL" when args.Length > 2:
							return await ResponseFindAndRemoveListings(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, " ")).ConfigureAwait(false);
						
						case "GEMS" or "GEM":
							return await ResponseGems(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
						
						case "KEYS" or "KEY":
							return await ResponseKeys(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
						
						case "LISTINGS" or "LISTING":
							return await ResponseListings(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
						
						case "LOGBOOSTERDATA" or "SENDBOOSTERDATA" or "LOGBOOSTERS" or "SENDBOOSTERS" or "LOGBD" or "SENDBD" or "LOGB" or "SENDB":
							return await ResponseLogBoosterData(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);

						case "LOGDATA" or "SENDDATA":
							return await ResponseLogData(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);

						case "LOGSTOP" or "STOPLOG" or "LOGDATASTOP" or "SENDDATASTOP" or "STOPLOGDATA" or "STOPSENDDATA":
							return ResponseLogStop(access, steamID, Utilities.GetArgsAsText(args, 1, ","));
						
						case "LOGINVENTORYHISTORY" or "SENDINVENTORYHISTORY" or "LOGIH" or "SENDIH" when args.Length > 5:
							return await ResponseLogInventoryHistory(access, steamID, bot, args[1], args[2], args[3], args[4], args[5]).ConfigureAwait(false);
						case "LOGINVENTORYHISTORY" or "SENDINVENTORYHISTORY" or "LOGIH" or "SENDIH" when args.Length > 3:
							return await ResponseLogInventoryHistory(access, steamID, bot, args[1], args[2], args[3]).ConfigureAwait(false);
						case "LOGINVENTORYHISTORY" or "SENDINVENTORYHISTORY" or "LOGIH" or "SENDIH" when args.Length > 2:
							return await ResponseLogInventoryHistory(access, steamID, bot, args[1], args[2]).ConfigureAwait(false);
						case "LOGINVENTORYHISTORY" or "SENDINVENTORYHISTORY" or "LOGIH" or "SENDIH":
							return await ResponseLogInventoryHistory(access, steamID, bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);	

						case "LOGMARKETHISTORY" or "SENDMARKETHISTORY" or "LOGMH" or "SENDMH" when args.Length > 3:
							return await ResponseLogMarketHistory(access, steamID, args[1], args[2], args[3]).ConfigureAwait(false);
						case "LOGMARKETHISTORY" or "SENDMARKETHISTORY" or "LOGMH" or "SENDMH" when args.Length > 2:
							return await ResponseLogMarketHistory(access, steamID, args[1], args[2]).ConfigureAwait(false);
						case "LOGMARKETHISTORY" or "SENDMARKETHISTORY" or "LOGMH" or "SENDMH":
							return await ResponseLogMarketHistory(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);

						case "LOGMARKETLISTINGS" or "SENDMARKETLISTINGS" or "LOGML" or "SENDML":
							return await ResponseLogMarketListings(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
						
						case "LOOTBOOSTERS" or "LOOTBOOSTER":
							return await ResponseSendItems(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.BoosterPack).ConfigureAwait(false);
						
						case "LOOTCARDS" or "LOOTCARD":
							return await ResponseSendItems(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.TradingCard).ConfigureAwait(false);
						
						case "LOOTFOILS" or "LOOTFOIL":
							return await ResponseSendItems(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard).ConfigureAwait(false);
						
						case "LOOTGEMS" or "LOOTGEM":
							return await ResponseSendItems(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.GemsClassID, allowUnmarketable: true).ConfigureAwait(false);
						
						case "LOOTITEMS" or "LOOTITEM" when args.Length > 4:
							return await ResponseSendSpecificItems(access, steamID, args[1], args[2], args[3], args[4]).ConfigureAwait(false);
						case "LOOTITEMS" or "LOOTITEM" when args.Length > 3:
							return await ResponseSendSpecificItems(access, steamID, args[1], args[2], args[3]).ConfigureAwait(false);
						
						case "LOOTKEYS" or "LOOTKEY":
							return await ResponseSendItems(access, steamID, Utilities.GetArgsAsText(args, 1, ","), KeyHandler.KeyAppID, KeyHandler.KeyContextID, KeyHandler.KeyType, marketHash: KeyHandler.MarketHash).ConfigureAwait(false);
						
						case "LOOTSACKS" or "LOOTSACK":
							return await ResponseSendItems(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.SackOfGemsClassID).ConfigureAwait(false);
						
						case "MARKET2FAOK" or "M2FAOK":
							return await Response2FAOK(access, steamID, args[1], Confirmation.EConfirmationType.Market).ConfigureAwait(false);
						
						case "TRADE2FAOK" or "T2FAOK":
							return await Response2FAOK(access, steamID, args[1], Confirmation.EConfirmationType.Trade).ConfigureAwait(false);
						
						case "TBA":
							return await ResponseSendItems(access, steamID, "ASF", Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.BoosterPack, recieverBotName: args[1]).ConfigureAwait(false);
						case "TRANSFERBOOSTERS" or "TRANSFERBOOSTER" when args.Length > 2:
							return await ResponseSendItems(access, steamID, args[1], Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.BoosterPack, recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERBOOSTERS" or "TRANSFERBOOSTER":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard, recieverBotName: args[1]).ConfigureAwait(false);
						
						case "TCA":
							return await ResponseSendItems(access, steamID, "ASF", Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.TradingCard, recieverBotName: args[1]).ConfigureAwait(false);
						case "TRANSFERCARDS" or "TRANSFERCARD" when args.Length > 2:
							return await ResponseSendItems(access, steamID, args[1], Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.TradingCard, recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERCARDS" or "TRANSFERCARD":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard, recieverBotName: args[1]).ConfigureAwait(false);
						
						case "TFA":
							return await ResponseSendItems(access, steamID, "ASF", Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard, recieverBotName: args[1]).ConfigureAwait(false);
						case "TRANSFERFOILS" or "TRANSFERFOIL" when args.Length > 2:
							return await ResponseSendItems(access, steamID, args[1], Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard, recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERFOILS" or "TRANSFERFOIL":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard, recieverBotName: args[1]).ConfigureAwait(false);
						
						case "TRANSFERGEMS" or "TRANSFERGEM" when args.Length > 3:
							return await ResponseSendItemsWithAmounts(access, steamID, args[1], args[2], args[3], Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.GemsClassID, allowUnmarketable: true).ConfigureAwait(false);
						case "TRANSFERGEMS" or "TRANSFERGEM" when args.Length > 2:
							return await ResponseSendItemsWithAmounts(bot, access, args[1], args[2], Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.GemsClassID, allowUnmarketable: true).ConfigureAwait(false);
						
						case "TRANSFERITEMS" or "TRANSFERITEM" when args.Length > 5:
							return await ResponseSendSpecificItems(access, steamID, args[1], args[3], args[4], args[5], recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERITEMS" or "TRANSFERITEM" when args.Length > 4:
							return await ResponseSendSpecificItems(access, steamID, args[1], args[3], args[4], recieverBotName: args[2]).ConfigureAwait(false);

						case "TRANSFERITEMS^" or "TRANSFERITEM^" when args.Length > 4:
							return await ResponseSendMultipleItems(access, steamID, args[1], args[2], args[3], Utilities.GetArgsAsText(args, 4, " ")).ConfigureAwait(false);
						
						case "TRANSFERKEYS" or "TRANSFERKEY" when args.Length > 3:
							return await ResponseSendItemsWithAmounts(access, steamID, args[1], args[2], args[3], KeyHandler.KeyAppID, KeyHandler.KeyContextID, KeyHandler.KeyType, marketHash: KeyHandler.MarketHash).ConfigureAwait(false);
						case "TRANSFERKEYS" or "TRANSFERKEY" when args.Length > 2:
							return await ResponseSendItemsWithAmounts(bot, access, args[1], args[2], KeyHandler.KeyAppID, KeyHandler.KeyContextID, KeyHandler.KeyType, marketHash: KeyHandler.MarketHash).ConfigureAwait(false);
						
						case "TRANSFERSACKS" or "TRANSFERSACK" when args.Length > 2:
							return await ResponseSendItems(access, steamID, args[1], Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.SteamGems, GemHandler.SackOfGemsClassID, recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERSACKS" or "TRANSFERSACK":
							return await ResponseSendItems(bot, access, Asset.SteamAppID, Asset.SteamCommunityContextID, Asset.EType.FoilTradingCard, recieverBotName: args[1]).ConfigureAwait(false);
						
						case "REMOVELISTINGS" or "REMOVELISTING" or "CANCELLISTINGS" or "CANCELLISTING" or "RLISTINGS" or "RLISTING" or "REMOVEL" when args.Length > 2:
							return await ResponseRemoveListings(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false);
						case "REMOVELISTINGS" or "REMOVELISTING" or "CANCELLISTINGS" or "CANCELLISTING" or "RLISTINGS" or "RLISTING" or "REMOVEL":
							return await ResponseRemoveListings(bot, access, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
						
						case "UNPACKGEMS" or "UNPACKGEM":
							return await ResponseUnpackGems(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
						
						case "VA":
							return await ResponseValue(access, steamID, "ASF", args[1]).ConfigureAwait(false);
						case "VALUE" when args.Length > 2:
							return await ResponseValue(access, steamID, args[1], args[2]).ConfigureAwait(false);
						case "VALUE":
							return await ResponseValue(access, steamID, args[1]).ConfigureAwait(false);
						
						default:
							return null;
					};
			}
		}

		private static async Task<string?> Response2FAOK(Bot bot, EAccess access, Confirmation.EConfirmationType acceptedType) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			if (!bot.HasMobileAuthenticator) {
				return FormatBotResponse(bot, Strings.BotNoASFAuthenticator);
			}

			(bool success, _, string message) = await bot.Actions.HandleTwoFactorAuthenticationConfirmations(true, acceptedType).ConfigureAwait(false);

			return FormatBotResponse(bot, success ? message : string.Format(Strings.WarningFailedWithError, message));
		}

		private static async Task<string?> Response2FAOK(EAccess access, ulong steamID, string botNames, Confirmation.EConfirmationType acceptedType) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => Response2FAOK(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), acceptedType))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
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

			string[] gameIDs = targetGameIDs.Split(",", StringSplitOptions.RemoveEmptyEntries);

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

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot => ResponseBooster(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), steamID, targetGameIDs, respondingBot));

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseBoosterStatus(Bot bot, EAccess access, bool shortStatus = false) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].GetStatus(shortStatus);
		}

		private static string? ResponseBoosterStatus(EAccess access, ulong steamID, string botNames, bool shortStatus = false) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot => ResponseBoosterStatus(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), shortStatus));

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

			string[] gameIDs = targetGameIDs.Split(",", StringSplitOptions.RemoveEmptyEntries);

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

		private static async Task<string?> ResponseFindListings(Bot bot, EAccess access, string itemIdentifiersAsText) {
			if (String.IsNullOrEmpty(itemIdentifiersAsText)) {
				throw new ArgumentNullException(nameof(itemIdentifiersAsText));
			}

			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			List<string> itemIdentifierStrings = itemIdentifiersAsText.Split("&&", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			List<ItemIdentifier> itemIdentifiers = new List<ItemIdentifier>();
			foreach (string itemIdentifierString in itemIdentifierStrings) {
				try {
					ItemIdentifier itemIdentifier = new ItemIdentifier(itemIdentifierString);
					itemIdentifiers.Add(itemIdentifier);
				} catch (Exception) {
					return FormatBotResponse(bot, String.Format("Invalid Item Identifier: {0}", itemIdentifierString));
				}
			}

			return await MarketHandler.FindListings(bot, itemIdentifiers).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseFindListings(EAccess access, ulong steamID, string botNames, string itemIdentifiersAsText) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseFindListings(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), itemIdentifiersAsText))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseFindAndRemoveListings(Bot bot, EAccess access, string itemIdentifiersAsText) {
			if (String.IsNullOrEmpty(itemIdentifiersAsText)) {
				throw new ArgumentNullException(nameof(itemIdentifiersAsText));
			}
			
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			List<string> itemIdentifierStrings = itemIdentifiersAsText.Split("&&", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			List<ItemIdentifier> itemIdentifiers = new List<ItemIdentifier>();
			foreach (string itemIdentifierString in itemIdentifierStrings) {
				try {
					ItemIdentifier itemIdentifier = new ItemIdentifier(itemIdentifierString);
					itemIdentifiers.Add(itemIdentifier);
				} catch (Exception) {
					return FormatBotResponse(bot, String.Format("Invalid Item Identifier: {0}", itemIdentifierString));
				}
			}

			return await MarketHandler.FindAndRemoveListings(bot, itemIdentifiers).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseFindAndRemoveListings(EAccess access, ulong steamID, string botNames, string itemIdentifiersAsText) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseFindAndRemoveListings(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), itemIdentifiersAsText))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
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

		private static async Task<string?> ResponseKeys(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return await KeyHandler.GetKeyCount(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseKeys(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseKeys(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseListings(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return await MarketHandler.GetListings(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseListings(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseListings(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseLogBoosterData(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return await DataHandler.SendBoosterDataOnly(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseLogBoosterData(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseLogBoosterData(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseLogData(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return await DataHandler.SendAllData(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseLogData(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseLogData(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseLogStop(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return DataHandler.StopSend(bot);
		}

		private static string? ResponseLogStop(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot => ResponseLogStop(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)));

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseLogInventoryHistory(Bot bot, EAccess access, ulong steamID, string? numPagesString = null, string? startTimeString = null, string? timeFracString = null, string? sString = null, Bot? respondingBot = null) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			uint? numPages = null;
			if (numPagesString != null) {
				if (uint.TryParse(numPagesString, out uint outValue)) {
					numPages = outValue;
				} else {
					return FormatStaticResponse(String.Format(Strings.ErrorIsInvalid, nameof(numPages)));
				}
			}

			uint? startTime = null;
			if (startTimeString != null) {
				if (uint.TryParse(startTimeString, out uint outValue)) {
					startTime = outValue;
				} else {
					return FormatStaticResponse(String.Format(Strings.ErrorIsInvalid, nameof(startTime)));
				}
			}
			
			uint? timeFrac = null;
			if (timeFracString != null) {
				if (uint.TryParse(timeFracString, out uint outValue)) {
					timeFrac = outValue;
				} else {
					return FormatStaticResponse(String.Format(Strings.ErrorIsInvalid, nameof(timeFrac)));
				}
			}
			
			string? s = null;
			if (sString != null) {
				if (ulong.TryParse(sString, out ulong outValue)) {
					s = outValue.ToString();
				} else {
					return FormatStaticResponse(String.Format(Strings.ErrorIsInvalid, nameof(s)));
				}
			}

			return await DataHandler.SendInventoryHistoryOnly(bot, respondingBot ?? bot, steamID, numPages, startTime, timeFrac, s).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseLogInventoryHistory(EAccess access, ulong steamID, Bot respondingBot, string botNames, string? numPagesString = null, string? startTimeString = null, string? timeFracString = null, string? sString = null) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseLogInventoryHistory(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), steamID, numPagesString, startTimeString, timeFracString, sString, respondingBot))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseLogMarketHistory(Bot bot, EAccess access, string? numPagesString = null, string? startPageString = null) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			uint? numPages = null;
			if (numPagesString != null) {
				if (uint.TryParse(numPagesString, out uint outValue)) {
					numPages = outValue;
				} else {
					return FormatStaticResponse(String.Format(Strings.ErrorIsInvalid, nameof(numPages)));
				}
			}

			uint? startPage = null;
			if (startPageString != null) {
				if (uint.TryParse(startPageString, out uint outValue)) {
					if (outValue == 0) {
						return FormatStaticResponse(String.Format(Strings.ErrorIsInvalid, nameof(startPage)));
					}

					startPage = outValue - 1;
				} else {
					return FormatStaticResponse(String.Format(Strings.ErrorIsInvalid, nameof(startPage)));
				}
			}

			return await DataHandler.SendMarketHistoryOnly(bot, numPages, startPage).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseLogMarketHistory(EAccess access, ulong steamID, string botNames, string? numPagesString = null, string? startPageString = null) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseLogMarketHistory(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), numPagesString, startPageString))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseLogMarketListings(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return await DataHandler.SendMarketListingsOnly(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseLogMarketListings(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseLogMarketListings(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseRemoveListings(Bot bot, EAccess access, string listingIDsString) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			string[] listingIDsStringArray = listingIDsString.Split(",", StringSplitOptions.RemoveEmptyEntries);

			if (listingIDsStringArray.Length == 0) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsEmpty, nameof(listingIDsStringArray)));
			}

			List<ulong> listingIDs = new List<ulong>();

			foreach (string listingIDString in listingIDsStringArray) {
				if (!ulong.TryParse(listingIDString, out ulong listingID)) {
					return FormatBotResponse(bot, String.Format(Strings.ErrorParsingObject, nameof(listingID)));
				}

				listingIDs.Add(listingID);
			}

			return await MarketHandler.RemoveListings(bot, listingIDs).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseRemoveListings(EAccess access, ulong steamID, string botName, string listingIDs) {
			if (String.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);

			if (bot == null) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botName)) : null;
			}

			return await ResponseRemoveListings(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), listingIDs).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseSendItems(Bot bot, EAccess access, uint appID, ulong contextID, Asset.EType? type, ulong? classID = null, string? marketHash = null, bool allowUnmarketable = false, string? recieverBotName = null) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			ulong targetSteamID = 0;

			if (recieverBotName != null) {
				Bot? reciever = Bot.GetBot(recieverBotName);

				if (reciever == null) {
					return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, recieverBotName)) : null;
				}

				targetSteamID = reciever.SteamID;

				if (!reciever.IsConnectedAndLoggedOn) {
					return FormatStaticResponse(Strings.BotNotConnected);
				}

				if (bot.SteamID == reciever.SteamID) {
					return FormatBotResponse(bot, Strings.BotSendingTradeToYourself);
				}
			}

			(bool success, string message) = await bot.Actions.SendInventory(appID: appID, contextID: contextID, targetSteamID: targetSteamID, 
				filterFunction: item => 
					(type == null || item.Type == type) 
					&& (classID == null || item.ClassID == classID) 
					&& (marketHash == null || ((item.AdditionalPropertiesReadOnly?.ContainsKey("market_hash_name") ?? false) && item.AdditionalPropertiesReadOnly?["market_hash_name"].ToObject<string>() == marketHash)) 
					&& (allowUnmarketable || item.Marketable)
			).ConfigureAwait(false);

			return FormatBotResponse(bot, success ? message : String.Format(Strings.WarningFailedWithError, message));
		}

		private static async Task<string?> ResponseSendItems(EAccess access, ulong steamID, string senderBotNames, uint appID, ulong contextID, Asset.EType type, ulong? classID = null, string? marketHash = null, bool allowUnmarketable = false, string? recieverBotName = null) {
			if (String.IsNullOrEmpty(senderBotNames)) {
				throw new ArgumentNullException(nameof(senderBotNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(senderBotNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, senderBotNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSendItems(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), appID, contextID, type, classID, marketHash, allowUnmarketable, recieverBotName))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseSendSpecificItems(Bot bot, EAccess access, string appIDAsText, string contextIDAsText, string? classIDAsText = null, string? recieverBotName = null) {
			if (String.IsNullOrEmpty(appIDAsText)) {
				throw new ArgumentNullException(nameof(appIDAsText));
			}

			if (String.IsNullOrEmpty(contextIDAsText)) {
				throw new ArgumentNullException(nameof(contextIDAsText));
			}
			
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			if (!uint.TryParse(appIDAsText, out uint appID)) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsInvalid, nameof(appIDAsText)));
			}

			if (!ulong.TryParse(contextIDAsText, out ulong contextID)) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsInvalid, nameof(contextIDAsText)));
			}

			ulong? classID = null;
			if (classIDAsText != null) {
				if (ulong.TryParse(classIDAsText, out ulong outValue)) {
					classID = outValue;
				} else {
					return FormatStaticResponse(String.Format(Strings.ErrorIsInvalid, nameof(classIDAsText)));
				}
			}

			return await ResponseSendItems(bot, access, appID, contextID, null, classID, allowUnmarketable: true, recieverBotName: recieverBotName).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseSendSpecificItems(EAccess access, ulong steamID, string senderBotNames, string appIDAsText, string contextIDAsText, string? classIDAsText = null, string? recieverBotName = null) {
			if (String.IsNullOrEmpty(senderBotNames)) {
				throw new ArgumentNullException(nameof(senderBotNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(senderBotNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, senderBotNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSendSpecificItems(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), appIDAsText, contextIDAsText, classIDAsText, recieverBotName))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseSendMultipleItems(Bot bot, EAccess access, string botNames, string amountsString, string itemIdentifiersAsText) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			if (String.IsNullOrEmpty(itemIdentifiersAsText)) {
				throw new ArgumentNullException(nameof(itemIdentifiersAsText));
			}

			if (String.IsNullOrEmpty(amountsString)) {
				throw new ArgumentNullException(nameof(amountsString));
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

			string[] itemIdentifierStrings = itemIdentifiersAsText.Split("&&", StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
			string[] amountStrings = amountsString.Split(",", StringSplitOptions.RemoveEmptyEntries);

			if (amountStrings.Length == 0) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsEmpty, nameof(amountStrings)));
			}

			if (amountStrings.Length == 1 && itemIdentifierStrings.Length > 1) {
				amountStrings = Enumerable.Repeat(amountStrings[0], itemIdentifierStrings.Length).ToArray();
			}
			
			if (amountStrings.Length != itemIdentifierStrings.Length) {
				return FormatBotResponse(bot, String.Format("Number of items ({0}) does not match number of item amounts ({1})", itemIdentifierStrings.Length, amountStrings.Length));
			}

			List<uint> amounts = new List<uint>();
			foreach (string amount in amountStrings) {
				if (!uint.TryParse(amount, out uint amountNum)) {
					return FormatBotResponse(bot, String.Format(Strings.ErrorParsingObject, nameof(amountNum)));
				}

				amounts.Add(amountNum);
			}

			List<ItemIdentifier> itemIdentifiers = new List<ItemIdentifier>();
			foreach (string itemIdentifierString in itemIdentifierStrings) {
				try {
					ItemIdentifier itemIdentifier = new ItemIdentifier(itemIdentifierString, requireNumericIDs: true);
					itemIdentifiers.Add(itemIdentifier);
				} catch (Exception) {
					return FormatBotResponse(bot, String.Format("Invalid Item Identifier: {0}", itemIdentifierString));
				}
			}

			List<(ItemIdentifier, uint)> items = Zip(itemIdentifiers, amounts).ToList();

			return await InventoryHandler.BatchSendMultipleItemsWithAmounts(bot, bots, items).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseSendMultipleItems(EAccess access, ulong steamID, string senderBotName, string botNames, string amountsString, string itemIdentifiersAsText) {
			if (String.IsNullOrEmpty(senderBotName)) {
				throw new ArgumentNullException(nameof(senderBotName));
			}

			Bot? sender = Bot.GetBot(senderBotName);

			if (sender == null) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, senderBotName)) : null;
			}

			return await ResponseSendMultipleItems(sender, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(sender, access, steamID), botNames, amountsString, itemIdentifiersAsText).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseSendItemsWithAmounts(Bot bot, EAccess access, string botNames, string amountsString, uint appID, ulong contextID, Asset.EType type, ulong? classID = null, string? marketHash = null, bool allowUnmarketable = false) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			if (String.IsNullOrEmpty(amountsString)) {
				throw new ArgumentNullException(nameof(amountsString));
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

			string[] amounts = amountsString.Split(",", StringSplitOptions.RemoveEmptyEntries);

			if (amounts.Length == 0) {
				return FormatBotResponse(bot, String.Format(Strings.ErrorIsEmpty, nameof(amounts)));
			}

			if (amounts.Length == 1 && bots.Count > 1) {
				amounts = Enumerable.Repeat(amounts[0], bots.Count).ToArray();
			}
			
			if (amounts.Length != bots.Count) {
				return FormatBotResponse(bot, String.Format("Number of recieving bots ({0}) does not match number of gem amounts ({1})", bots.Count, amounts.Length));
			}

			List<uint> amountNums = new List<uint>();
			int botIndex = 0;
			foreach (string amount in amounts) {
				if (!uint.TryParse(amount, out uint amountNum)) {
					if (type == Asset.EType.SteamGems && (amount.ToUpperInvariant() == "QUEUE" || amount.ToUpperInvariant() == "Q")) {
						amountNum = BoosterHandler.BoosterHandlers[bots.ElementAt(botIndex).BotName].GetGemsNeeded();
					} else {
						return FormatBotResponse(bot, String.Format(Strings.ErrorParsingObject, nameof(amountNum)));
					}
				}

				amountNums.Add(amountNum);
				botIndex++;
			}

			List<(Bot reciever, uint amount)> recievers = Zip(bots, amountNums).ToList();

			return await InventoryHandler.BatchSendItemsWithAmounts(bot, recievers, appID, contextID, type, classID, marketHash, allowUnmarketable).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseSendItemsWithAmounts(EAccess access, ulong steamID, string senderBotName, string botNames, string amountsString, uint appID, ulong contextID, Asset.EType type, ulong? classID = null, string? marketHash = null, bool allowUnmarketable = false) {
			if (String.IsNullOrEmpty(senderBotName)) {
				throw new ArgumentNullException(nameof(senderBotName));
			}

			Bot? sender = Bot.GetBot(senderBotName);

			if (sender == null) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, senderBotName)) : null;
			}

			return await ResponseSendItemsWithAmounts(sender, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(sender, access, steamID), botNames, amountsString, appID, contextID, type, classID, marketHash, allowUnmarketable).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseUnpackGems(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			return await GemHandler.UnpackGems(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseUnpackGems(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseUnpackGems(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseValue(Bot bot, EAccess access, string? subtractFromAsText = null) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, Strings.BotNotConnected);
			}

			uint subtractFrom = 0;
			if (subtractFromAsText != null) {
				if (!uint.TryParse(subtractFromAsText, out subtractFrom)) {
					return FormatBotResponse(bot, String.Format(Strings.ErrorParsingObject, nameof(subtractFrom)));
				}
			}

			return await MarketHandler.GetValue(bot, subtractFrom).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseValue(EAccess access, ulong steamID, string botNames, string? subtractFromAsText = null) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseValue(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), subtractFromAsText))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		internal static string FormatStaticResponse(string response) => ArchiSteamFarm.Steam.Interaction.Commands.FormatStaticResponse(response);
		internal static string FormatBotResponse(Bot bot, string response) => bot.Commands.FormatBotResponse(response);

		// https://learn.microsoft.com/en-us/archive/blogs/ericlippert/zip-me-up
		private static IEnumerable<(TFirst, TSecond)> Zip<TFirst, TSecond> (IEnumerable<TFirst> first, IEnumerable<TSecond> second) {
			using (IEnumerator<TFirst> e1 = first.GetEnumerator())
				using (IEnumerator<TSecond> e2 = second.GetEnumerator())
					while (e1.MoveNext() && e2.MoveNext())
						yield return (e1.Current, e2.Current);
		}
	}
}
