using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Data;
using System.ComponentModel;
using System.Reflection;
using BoosterManager.Localization;

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
						case "BOOSTERMANAGER" when access >= EAccess.FamilySharing:
							return String.Format("{0} {1}", nameof(BoosterManager), (typeof(BoosterManager).Assembly.GetName().Version ?? new Version("0")).ToString());

						case "BDROP" or "BDROPS":
							return await ResponseBoosterDrops(bot, access).ConfigureAwait(false);

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
						
						case "LBA" or "MLBA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULBA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALBA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString()).ConfigureAwait(false);
						case "LOOTBOOSTERS" or "LOOTBOOSTER" or "MLOOTBOOSTERS" or "MLOOTBOOSTER":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULOOTBOOSTERS" or "ULOOTBOOSTER":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALOOTBOOSTERS" or "ALOOTBOOSTER":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString()).ConfigureAwait(false);
						
						case "LCA" or "MLCA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULCA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALCA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString()).ConfigureAwait(false);
						case "LOOTCARDS" or "LOOTCARD" or "MLOOTCARDS" or "MLOOTCARD":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULOOTCARDS" or "ULOOTCARD":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALOOTCARDS" or "ALOOTCARD":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString()).ConfigureAwait(false);
						
						case "LFA" or "MLFA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULFA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALFA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString()).ConfigureAwait(false);
						case "LOOTFOILS" or "LOOTFOIL" or "MLOOTFOILS" or "MLOOTFOIL":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULOOTFOILS" or "ULOOTFOIL":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALOOTFOILS" or "ALOOTFOIL":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString()).ConfigureAwait(false);
						
						case "LGA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.GemIdentifier.ToString()).ConfigureAwait(false);
						case "LOOTGEMS" or "LOOTGEM":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.GemIdentifier.ToString()).ConfigureAwait(false);
						
						case "LKA":
							return await ResponseSendItemToBot(access, steamID, "ASF", KeyHandler.KeyAppID.ToString(), KeyHandler.KeyContextID.ToString(), ItemIdentifier.KeyIdentifier.ToString()).ConfigureAwait(false);
						case "LOOTKEYS" or "LOOTKEY":
							return await ResponseSendItemToBot(bot, access, KeyHandler.KeyAppID.ToString(), KeyHandler.KeyContextID.ToString(), ItemIdentifier.KeyIdentifier.ToString()).ConfigureAwait(false);
						
						case "LSA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.SackIdentifier.ToString()).ConfigureAwait(false);
						case "LOOTSACKS" or "LOOTSACK":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.SackIdentifier.ToString()).ConfigureAwait(false);
						
						case "M2FAOKA":
							return await Response2FAOK(access, steamID, "ASF", Confirmation.EConfirmationType.Market).ConfigureAwait(false);
						case "MARKET2FAOK" or "M2FAOK":
							return await Response2FAOK(bot, access, Confirmation.EConfirmationType.Market).ConfigureAwait(false);

						case "T2FAOKA":
							return await Response2FAOK(access, steamID, "ASF", Confirmation.EConfirmationType.Trade).ConfigureAwait(false);
						case "TRADE2FAOK" or "T2FAOK":
							return await Response2FAOK(bot, access, Confirmation.EConfirmationType.Trade).ConfigureAwait(false);

						case "TCA" or "TCHECKA" or "TRADECHECKA":
							return ResponseTradeCheck(access, steamID, "ASF");
						case "TRADECHECK" or "TCHECK" or "TC":
							return ResponseTradeCheck(bot, access);

						case "TIAA" or "TINCAA" or "TRADESINCOMINGAA" or "TRADEINCOMINGAA" or "TRADESINCOMMINGAA" or "TRADEINCOMMINGAA" or "TRADEIAA" or "TRADESIAA" or "TRADESINCAA" or "TRADEINCAA":
							return await ResponseTradeCount(access, steamID, "ASF", "ASF").ConfigureAwait(false);
						case "TIA" or "TINCA" or "TRADESINCOMINGA" or "TRADEINCOMINGA" or "TRADESINCOMMINGA" or "TRADEINCOMMINGA" or "TRADEIA" or "TRADESIA" or "TRADESINCA" or "TRADEINCA":
							return await ResponseTradeCount(access, steamID, "ASF").ConfigureAwait(false);
						case "TI" or "TINC" or "TRADESINCOMING" or "TRADEINCOMING" or "TRADESINCOMMING" or "TRADEINCOMMING" or "TRADEI" or "TRADESI" or "TRADESINC" or "TRADEINC":
							return await ResponseTradeCount(bot, access).ConfigureAwait(false);

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

						case "BDROP" or "BDROPS":
							return await ResponseBoosterDrops(access, steamID, args[1]).ConfigureAwait(false);

						case "BRATE" or "BOOSTERRATE":
							return ResponseBoosterRate(access, args[1]);
						
						case "BSTATUS" or "BOOSTERSTATUS":
							return ResponseBoosterStatus(access, steamID, args[1]);

						case "BSTATUS^" or "BOOSTERSTATUS^":
							return ResponseBoosterStatus(access, steamID, args[1], true);
						
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
						
						case "FINDLISTINGS" or "FINDLISTING" or "FLISTINGS" or "FLISTING" or "FINDL" or "FL" when args.Length > 2:
							return await ResponseFindListings(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, " ")).ConfigureAwait(false);
						
						case "FINDANDREMOVELISTINGS" or "FINDANDREMOVELISTING" or "FINDREMOVELISTINGS" or "FINDREMOVELISTING" or "FINDANDCANCELLISTINGS" or "FINDANDCANCELLISTING" or "FINDCANCELLISTINGS" or "FINDCANCELLISTING" or "FRLISTINGS" or "FRLISTING" or "FINDREMOVEL" or "FRL" when args.Length > 2:
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
							return await ResponseLogInventoryHistory(access, steamID, bot, args[1]).ConfigureAwait(false);	

						case "LOGMARKETHISTORY" or "SENDMARKETHISTORY" or "LOGMH" or "SENDMH" when args.Length > 3:
							return await ResponseLogMarketHistory(access, steamID, args[1], args[2], args[3]).ConfigureAwait(false);
						case "LOGMARKETHISTORY" or "SENDMARKETHISTORY" or "LOGMH" or "SENDMH" when args.Length > 2:
							return await ResponseLogMarketHistory(access, steamID, args[1], args[2]).ConfigureAwait(false);
						case "LOGMARKETHISTORY" or "SENDMARKETHISTORY" or "LOGMH" or "SENDMH":
							return await ResponseLogMarketHistory(access, steamID, args[1]).ConfigureAwait(false);

						case "LOGMARKETLISTINGS" or "SENDMARKETLISTINGS" or "LOGML" or "SENDML":
							return await ResponseLogMarketListings(access, steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
						
						case "LOOTBOOSTERS" or "LOOTBOOSTER" or "MLOOTBOOSTERS" or "MLOOTBOOSTER":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULOOTBOOSTERS" or "ULOOTBOOSTER":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALOOTBOOSTERS" or "ALOOTBOOSTER":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString()).ConfigureAwait(false);
						
						case "LOOTCARDS" or "LOOTCARD" or "MLOOTCARDS" or "MLOOTCARD":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULOOTCARDS" or "ULOOTCARD":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALOOTCARDS" or "ALOOTCARD":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString()).ConfigureAwait(false);
						
						case "LOOTFOILS" or "LOOTFOIL" or "MLOOTFOILS" or "MLOOTFOIL":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: true).ConfigureAwait(false);
						case "ULOOTFOILS" or "ULOOTFOIL":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: false).ConfigureAwait(false);
						case "ALOOTFOILS" or "ALOOTFOIL":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString()).ConfigureAwait(false);
						
						case "LOOTGEMS" or "LOOTGEM":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.GemIdentifier.ToString()).ConfigureAwait(false);
						
						case "LOOTITEMS" or "LOOTITEM" or "ALOOTITEMS" or "ALOOTITEM" when args.Length > 4:
							return await ResponseSendItemToBot(access, steamID, args[1], args[2], args[3], Utilities.GetArgsAsText(args, 4, " ")).ConfigureAwait(false);
						case "ULOOTITEMS" or "ULOOTITEM" when args.Length > 4:
							return await ResponseSendItemToBot(access, steamID, args[1], args[2], args[3], Utilities.GetArgsAsText(args, 4, " "), marketable: false).ConfigureAwait(false);
						case "MLOOTITEMS" or "MLOOTITEM" when args.Length > 4:
							return await ResponseSendItemToBot(access, steamID, args[1], args[2], args[3], Utilities.GetArgsAsText(args, 4, " "), marketable: true).ConfigureAwait(false);
						
						case "LOOTKEYS" or "LOOTKEY":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), KeyHandler.KeyAppID.ToString(), KeyHandler.KeyContextID.ToString(), ItemIdentifier.KeyIdentifier.ToString()).ConfigureAwait(false);
						
						case "LOOTSACKS" or "LOOTSACK":
							return await ResponseSendItemToBot(access, steamID, Utilities.GetArgsAsText(args, 1, ","), Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.SackIdentifier.ToString()).ConfigureAwait(false);
						
						case "MARKET2FAOK" or "M2FAOK" when args.Length > 2:
							return await Response2FAOK(access, steamID, args[1], Confirmation.EConfirmationType.Market, args[2]).ConfigureAwait(false);
						case "MARKET2FAOK" or "M2FAOK":
							return await Response2FAOK(access, steamID, args[1], Confirmation.EConfirmationType.Market).ConfigureAwait(false);
						case "MARKET2FAOKA" or "M2FAOKA":
							return await Response2FAOK(access, steamID, "ASF", Confirmation.EConfirmationType.Market, args[1]).ConfigureAwait(false);
						
						case "TRADE2FAOK" or "T2FAOK":
							return await Response2FAOK(access, steamID, args[1], Confirmation.EConfirmationType.Trade).ConfigureAwait(false);

						case "TRADECHECK" or "TCHECK" or "TC":
							return ResponseTradeCheck(access, steamID, args[1]);

						case "TI" or "TINC" or "TRADESINCOMING" or "TRADEINCOMING" or "TRADESINCOMMING" or "TRADEINCOMMING" or "TRADEI" or "TRADESI" or "TRADESINC" or "TRADEINC" when args.Length > 2:
							return await ResponseTradeCount(access, steamID, args[1], args[2]).ConfigureAwait(false);
						case "TI" or "TINC" or "TRADESINCOMING" or "TRADEINCOMING" or "TRADESINCOMMING" or "TRADEINCOMMING" or "TRADEI" or "TRADESI" or "TRADESINC" or "TRADEINC":
							return await ResponseTradeCount(access, steamID, args[1]).ConfigureAwait(false);
						
						case "TBA" or "MTBA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: true, recieverBotName: args[1]).ConfigureAwait(false);
						case "UTBA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: false, recieverBotName: args[1]).ConfigureAwait(false);
						case "ATBA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						case "TRANSFERBOOSTERS" or "TRANSFERBOOSTER" or "MTRANSFERBOOSTERS" or "MTRANSFERBOOSTER" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: true, recieverBotName: args[2]).ConfigureAwait(false);
						case "UTRANSFERBOOSTERS" or "UTRANSFERBOOSTER" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: false, recieverBotName: args[2]).ConfigureAwait(false);
						case "ATRANSFERBOOSTERS" or "ATRANSFERBOOSTER" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERBOOSTERS" or "TRANSFERBOOSTER" or "MTRANSFERBOOSTERS" or "MTRANSFERBOOSTER":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: true, recieverBotName: args[1]).ConfigureAwait(false);
						case "UTRANSFERBOOSTERS" or "UTRANSFERBOOSTER":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), marketable: false, recieverBotName: args[1]).ConfigureAwait(false);
						case "ATRANSFERBOOSTERS" or "ATRANSFERBOOSTER":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.BoosterIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						
						case "TCA" or "MTCA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: true, recieverBotName: args[1]).ConfigureAwait(false);
						case "UTCA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: false, recieverBotName: args[1]).ConfigureAwait(false);
						case "ATCA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						case "TRANSFERCARDS" or "TRANSFERCARD" or "MTRANSFERCARDS" or "MTRANSFERCARD" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: true, recieverBotName: args[2]).ConfigureAwait(false);
						case "UTRANSFERCARDS" or "UTRANSFERCARD" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: false, recieverBotName: args[2]).ConfigureAwait(false);
						case "ATRANSFERCARDS" or "ATRANSFERCARD" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERCARDS" or "TRANSFERCARD" or "MTRANSFERCARDS" or "MTRANSFERCARD":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: true, recieverBotName: args[1]).ConfigureAwait(false);
						case "UTRANSFERCARDS" or "UTRANSFERCARD":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), marketable: false, recieverBotName: args[1]).ConfigureAwait(false);
						case "ATRANSFERCARDS" or "ATRANSFERCARD":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.CardIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						
						case "TFA" or "MTFA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: true, recieverBotName: args[1]).ConfigureAwait(false);
						case "UTFA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: false, recieverBotName: args[1]).ConfigureAwait(false);
						case "ATFA":
							return await ResponseSendItemToBot(access, steamID, "ASF", Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						case "TRANSFERFOILS" or "TRANSFERFOIL" or "MTRANSFERFOILS" or "MTRANSFERFOIL" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: true, recieverBotName: args[2]).ConfigureAwait(false);
						case "UTRANSFERFOILS" or "UTRANSFERFOIL" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: false, recieverBotName: args[2]).ConfigureAwait(false);
						case "ATRANSFERFOILS" or "ATRANSFERFOIL" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERFOILS" or "TRANSFERFOIL" or "MTRANSFERFOILS" or "MTRANSFERFOIL":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: true, recieverBotName: args[1]).ConfigureAwait(false);
						case "UTRANSFERFOILS" or "UTRANSFERFOIL":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), marketable: false, recieverBotName: args[1]).ConfigureAwait(false);
						case "ATRANSFERFOILS" or "ATRANSFERFOIL":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.FoilIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						
						case "TRANSFERGEMS" or "TRANSFERGEM" when args.Length > 3:
							return await ResponseSendItemToMultipleBots(access, steamID, args[1], args[2], args[3], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.GemIdentifier.ToString()).ConfigureAwait(false);
						case "TRANSFERGEMS" or "TRANSFERGEM" when args.Length > 2:
							return await ResponseSendItemToMultipleBots(bot, access, args[1], args[2], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.GemIdentifier.ToString()).ConfigureAwait(false);

						case "TRANSFERGEMS^" or "TRANSFERGEM^" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.GemIdentifier.ToString(), recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERGEMS^" or "TRANSFERGEM^":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.GemIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						
						case "TRANSFERITEMS" or "TRANSFERITEM" or "ATRANSFERITEMS" or "ATRANSFERITEM" when args.Length > 5:
							return await ResponseSendItemToBot(access, steamID, args[1], args[3], args[4], Utilities.GetArgsAsText(args, 5, " "), recieverBotName: args[2]).ConfigureAwait(false);
						case "UTRANSFERITEMS" or "UTRANSFERITEM" when args.Length > 5:
							return await ResponseSendItemToBot(access, steamID, args[1], args[3], args[4], Utilities.GetArgsAsText(args, 5, " "), recieverBotName: args[2], marketable: false).ConfigureAwait(false);
						case "MTRANSFERITEMS" or "MTRANSFERITEM" when args.Length > 5:
							return await ResponseSendItemToBot(access, steamID, args[1], args[3], args[4], Utilities.GetArgsAsText(args, 5, " "), recieverBotName: args[2], marketable: true).ConfigureAwait(false);

						case "TRANSFERITEMS^" or "TRANSFERITEM^" or "ATRANSFERITEMS^" or "ATRANSFERITEM^" when args.Length > 6:
							return await ResponseSendMultipleItemsToMultipleBots(access, steamID, args[1], args[2], args[3], args[4], args[5], Utilities.GetArgsAsText(args, 6, " ")).ConfigureAwait(false);
						case "UTRANSFERITEMS^" or "UTRANSFERITEM^" when args.Length > 6:
							return await ResponseSendMultipleItemsToMultipleBots(access, steamID, args[1], args[2], args[3], args[4], args[5], Utilities.GetArgsAsText(args, 6, " "), marketable: false).ConfigureAwait(false);
						case "MTRANSFERITEMS^" or "MTRANSFERITEM^" when args.Length > 6:
							return await ResponseSendMultipleItemsToMultipleBots(access, steamID, args[1], args[2], args[3], args[4], args[5], Utilities.GetArgsAsText(args, 6, " "), marketable: true).ConfigureAwait(false);

						case "TRANSFERITEMS%" or "TRANSFERITEM%" or "ATRANSFERITEMS%" or "ATRANSFERITEM%" when args.Length > 6:
							return await ResponseSendItemToMultipleBots(access, steamID, args[1], args[2], args[3], args[4], args[5], Utilities.GetArgsAsText(args, 6, " ")).ConfigureAwait(false);
						case "UTRANSFERITEMS%" or "UTRANSFERITEM%" when args.Length > 6:
							return await ResponseSendItemToMultipleBots(access, steamID, args[1], args[2], args[3], args[4], args[5], Utilities.GetArgsAsText(args, 6, " "), marketable: false).ConfigureAwait(false);
						case "MTRANSFERITEMS%" or "MTRANSFERITEM%" when args.Length > 6:
							return await ResponseSendItemToMultipleBots(access, steamID, args[1], args[2], args[3], args[4], args[5], Utilities.GetArgsAsText(args, 6, " "), marketable: true).ConfigureAwait(false);
						
						case "TRANSFERKEYS" or "TRANSFERKEY" when args.Length > 3:
							return await ResponseSendItemToMultipleBots(access, steamID, args[1], args[2], args[3], KeyHandler.KeyAppID.ToString(), KeyHandler.KeyContextID.ToString(), ItemIdentifier.KeyIdentifier.ToString()).ConfigureAwait(false);
						case "TRANSFERKEYS" or "TRANSFERKEY" when args.Length > 2:
							return await ResponseSendItemToMultipleBots(bot, access, args[1], args[2], KeyHandler.KeyAppID.ToString(), KeyHandler.KeyContextID.ToString(), ItemIdentifier.KeyIdentifier.ToString()).ConfigureAwait(false);

						case "TRANSFERKEYS^" or "TRANSFERKEY^" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], KeyHandler.KeyAppID.ToString(), KeyHandler.KeyContextID.ToString(), ItemIdentifier.KeyIdentifier.ToString(), recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERKEYS^" or "TRANSFERKEY^":
							return await ResponseSendItemToBot(bot, access, KeyHandler.KeyAppID.ToString(), KeyHandler.KeyContextID.ToString(), ItemIdentifier.KeyIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						
						case "TRANSFERSACKS" or "TRANSFERSACK" when args.Length > 2:
							return await ResponseSendItemToBot(access, steamID, args[1], Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.SackIdentifier.ToString(), recieverBotName: args[2]).ConfigureAwait(false);
						case "TRANSFERSACKS" or "TRANSFERSACK":
							return await ResponseSendItemToBot(bot, access, Asset.SteamAppID.ToString(), Asset.SteamCommunityContextID.ToString(), ItemIdentifier.SackIdentifier.ToString(), recieverBotName: args[1]).ConfigureAwait(false);
						
						case "REMOVELISTINGS" or "REMOVELISTING" or "CANCELLISTINGS" or "CANCELLISTING" or "RLISTINGS" or "RLISTING" or "REMOVEL" or "RL" when args.Length > 2:
							return await ResponseRemoveListings(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false);
						case "REMOVELISTINGS" or "REMOVELISTING" or "CANCELLISTINGS" or "CANCELLISTING" or "RLISTINGS" or "RLISTING" or "REMOVEL" or "RL" :
							return await ResponseRemoveListings(bot, access, args[1]).ConfigureAwait(false);
						
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

		private static async Task<string?> Response2FAOK(Bot bot, EAccess access, Confirmation.EConfirmationType acceptedType, string? minutesAsText = null) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			if (!bot.HasMobileAuthenticator) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNoASFAuthenticator);
			}

			string? repeatMessage = null;
			if (minutesAsText != null && acceptedType == Confirmation.EConfirmationType.Market) {
				if (!uint.TryParse(minutesAsText, out uint minutes)) {
					return String.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(minutesAsText));
				}

				if (minutes == 0) {
					if (BoosterHandler.BoosterHandlers[bot.BotName].StopMarketTimer()) {
						return FormatBotResponse(bot, Strings.RepetitionCancelled);
					} else {
						return FormatBotResponse(bot, Strings.RepetitionNotActive);
					}
				} else {
					BoosterHandler.BoosterHandlers[bot.BotName].StartMarketTimer(minutes);
					repeatMessage = String.Format(Strings.RepetitionNotice, minutes, String.Format("!m2faok {0} 0", bot.BotName));
				}
			}
			
			(bool success, _, string message) = await bot.Actions.HandleTwoFactorAuthenticationConfirmations(true, acceptedType).ConfigureAwait(false);
			string twofacMessage = success ? message : String.Format(ArchiSteamFarm.Localization.Strings.WarningFailedWithError, message);

			if (repeatMessage != null) {
				return FormatBotResponse(bot, String.Format("{0}. {1}", twofacMessage, repeatMessage));
			}

			return FormatBotResponse(bot, twofacMessage);
		}

		private static async Task<string?> Response2FAOK(EAccess access, ulong steamID, string botNames, Confirmation.EConfirmationType acceptedType, string? minutesAsText = null) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => Response2FAOK(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), acceptedType, minutesAsText))).ConfigureAwait(false);

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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			string[] gameIDs = targetGameIDs.Split(",", StringSplitOptions.RemoveEmptyEntries);

			if (gameIDs.Length == 0) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsEmpty, nameof(gameIDs)));
			}

			HashSet<uint> gamesToBooster = new HashSet<uint>();

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(gameID)));
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
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot => ResponseBooster(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), steamID, targetGameIDs, respondingBot));

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseBoosterDrops(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			var eligibleBoosters = await bot.ArchiWebHandler.GetBoosterEligibility().ConfigureAwait(false);
			if (eligibleBoosters == null) {
				return FormatBotResponse(bot, Strings.EligibleBoosterFetchFailed);
			}

			return FormatBotResponse(bot, String.Format(Strings.EligibleBoosterCount, eligibleBoosters.Count));
		}

		private static async Task<string?> ResponseBoosterDrops(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseBoosterDrops(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static string? ResponseBoosterRate(EAccess access, string levelString) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!uint.TryParse(levelString, out uint level)) {
				return String.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(levelString));
			}

			// https://steamcommunity.com/groups/BadgesCollectors/discussions/0/630800444048297919/?ctp=19#c2686880925148364340
			// If you're getting boosters less often than than this: your booster drop rate is capped by the number of booster eligible games
			// If you're getting boosters around as often as this: your booster drop rate is capped by your level
			double hoursPerBooster = (14*24) / (1 + (level / 50.0));

			if (hoursPerBooster < 24) {
				return String.Format(Strings.BoosterRateHours, level, String.Format("{0:0.##}", hoursPerBooster));
			} else {
				return String.Format(Strings.BoosterRateDays, level, String.Format("{0:0.##}", hoursPerBooster / 24));
			}
		}

		private static string? ResponseBoosterStatus(Bot bot, EAccess access, bool shortStatus = false) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].GetStatus(shortStatus);
		}

		private static string? ResponseBoosterStatus(EAccess access, ulong steamID, string botNames, bool shortStatus = false) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			string[] gameIDs = targetGameIDs.Split(",", StringSplitOptions.RemoveEmptyEntries);

			if (gameIDs.Length == 0) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsEmpty, nameof(gameIDs)));
			}

			HashSet<uint> gamesToStop = new HashSet<uint>();

			foreach (string game in gameIDs) {
				if (!uint.TryParse(game, out uint gameID) || (gameID == 0)) {
					return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(gameID)));
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
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			if (!uint.TryParse(timeLimit, out uint timeLimitHours)) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(timeLimit)));
			}

			return BoosterHandler.BoosterHandlers[bot.BotName].UnscheduleBoosters(timeLimitHours: (int) timeLimitHours);
		}

		private static string? ResponseBoosterStopTime(EAccess access, ulong steamID, string botNames, string timeLimit) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			List<string> itemIdentifierStrings = itemIdentifiersAsText.Split(ItemIdentifier.Delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			List<ItemIdentifier> itemIdentifiers = new List<ItemIdentifier>();
			foreach (string itemIdentifierString in itemIdentifierStrings) {
				try {
					itemIdentifiers.Add(new ItemIdentifier(itemIdentifierString));
				} catch (Exception) {
					return FormatBotResponse(bot, String.Format(Strings.InvalidItemIdentifier, itemIdentifierString));
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
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			List<string> itemIdentifierStrings = itemIdentifiersAsText.Split(ItemIdentifier.Delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			List<ItemIdentifier> itemIdentifiers = new List<ItemIdentifier>();
			foreach (string itemIdentifierString in itemIdentifierStrings) {
				try {
					itemIdentifiers.Add(new ItemIdentifier(itemIdentifierString));
				} catch (Exception) {
					return FormatBotResponse(bot, String.Format(Strings.InvalidItemIdentifier, itemIdentifierString));
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
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return await GemHandler.GetGemCount(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseGems(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return await KeyHandler.GetKeyCount(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseKeys(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return await MarketHandler.GetListings(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseListings(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return await DataHandler.SendBoosterDataOnly(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseLogBoosterData(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return await DataHandler.SendAllData(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseLogData(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return DataHandler.StopSend(bot);
		}

		private static string? ResponseLogStop(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			uint? numPages = null;
			if (numPagesString != null) {
				if (uint.TryParse(numPagesString, out uint outValue)) {
					numPages = outValue;
				} else {
					return FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(numPages)));
				}
			}

			uint? startTime = null;
			if (startTimeString != null) {
				if (uint.TryParse(startTimeString, out uint outValue)) {
					startTime = outValue;
				} else {
					return FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(startTime)));
				}
			}
			
			uint? timeFrac = null;
			if (timeFracString != null) {
				if (uint.TryParse(timeFracString, out uint outValue)) {
					timeFrac = outValue;
				} else {
					return FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(timeFrac)));
				}
			}
			
			string? s = null;
			if (sString != null) {
				if (ulong.TryParse(sString, out ulong outValue)) {
					s = outValue.ToString();
				} else {
					return FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(s)));
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
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			uint? numPages = null;
			if (numPagesString != null) {
				if (uint.TryParse(numPagesString, out uint outValue)) {
					numPages = outValue;
				} else {
					return FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(numPages)));
				}
			}

			uint? startPage = null;
			if (startPageString != null) {
				if (uint.TryParse(startPageString, out uint outValue)) {
					if (outValue == 0) {
						return FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(startPage)));
					}

					startPage = outValue - 1;
				} else {
					return FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(startPage)));
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
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return await DataHandler.SendMarketListingsOnly(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseLogMarketListings(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			string[] listingIDsStringArray = listingIDsString.Split(",", StringSplitOptions.RemoveEmptyEntries);

			if (listingIDsStringArray.Length == 0) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsEmpty, nameof(listingIDsStringArray)));
			}

			List<ulong> listingIDs = new List<ulong>();

			foreach (string listingIDString in listingIDsStringArray) {
				if (!ulong.TryParse(listingIDString, out ulong listingID)) {
					return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(listingID)));
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
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botName)) : null;
			}

			return await ResponseRemoveListings(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), listingIDs).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseSendItemToBot(Bot bot, EAccess access, string appIDAsText, string contextIDAsText, string itemIdentifiersAsText, bool? marketable = null, string? recieverBotName = null) {
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			if (!uint.TryParse(appIDAsText, out uint appID)) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(appIDAsText)));
			}

			if (!ulong.TryParse(contextIDAsText, out ulong contextID)) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(contextIDAsText)));
			}

			if (String.IsNullOrEmpty(itemIdentifiersAsText)) {
				throw new ArgumentNullException(nameof(itemIdentifiersAsText));
			}

			List<string> itemIdentifierStrings = itemIdentifiersAsText.Split(ItemIdentifier.Delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			List<ItemIdentifier> itemIdentifiers = new List<ItemIdentifier>();
			foreach (string itemIdentifierString in itemIdentifierStrings) {
				try {
					itemIdentifiers.Add(new ItemIdentifier(itemIdentifierString, marketable));
				} catch (Exception) {
					return FormatBotResponse(bot, String.Format(Strings.InvalidItemIdentifier, itemIdentifierString));
				}
			}

			ulong targetSteamID = 0;
			if (recieverBotName != null) {
				Bot? reciever = Bot.GetBot(recieverBotName);

				if (reciever == null) {
					return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, recieverBotName)) : null;
				}

				targetSteamID = reciever.SteamID;

				if (!reciever.IsConnectedAndLoggedOn) {
					return FormatStaticResponse(ArchiSteamFarm.Localization.Strings.BotNotConnected);
				}

				if (bot.SteamID == reciever.SteamID) {
					return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotSendingTradeToYourself);
				}
			}

			(bool success, string message) = await bot.Actions.SendInventory(appID: appID, contextID: contextID, targetSteamID: targetSteamID, filterFunction: item => itemIdentifiers.Any(itemIdentifier => itemIdentifier.IsItemMatch(item))).ConfigureAwait(false);

			return FormatBotResponse(bot, success ? message : String.Format(ArchiSteamFarm.Localization.Strings.WarningFailedWithError, message));
		}

		private static async Task<string?> ResponseSendItemToBot(EAccess access, ulong steamID, string senderBotNames, string appIDAsText, string contextIDAsText, string itemIdentifiersAsText, bool? marketable = null, string? recieverBotName = null) {
			if (String.IsNullOrEmpty(senderBotNames)) {
				throw new ArgumentNullException(nameof(senderBotNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(senderBotNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, senderBotNames)) : null;
			}

			// Send All of Item X to Bot C from Bots E,F,...
			// 	All of Item X to Bot C from Bot E
			// 	All of Item X to Bot C from Bot F
			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSendItemToBot(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), appIDAsText, contextIDAsText, itemIdentifiersAsText, marketable, recieverBotName))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? String.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseSendItemToMultipleBots(Bot bot, EAccess access, string recieverBotNames, string amountsAsText, string appIDAsText, string contextIDAsText, string itemIdentifierAsText, bool? marketable = null) {
			if (String.IsNullOrEmpty(appIDAsText)) {
				throw new ArgumentNullException(nameof(appIDAsText));
			}

			if (String.IsNullOrEmpty(contextIDAsText)) {
				throw new ArgumentNullException(nameof(contextIDAsText));
			}

			if (String.IsNullOrEmpty(amountsAsText)) {
				throw new ArgumentNullException(nameof(amountsAsText));
			}

			if (String.IsNullOrEmpty(itemIdentifierAsText)) {
				throw new ArgumentNullException(nameof(itemIdentifierAsText));
			}

			if (String.IsNullOrEmpty(recieverBotNames)) {
				throw new ArgumentNullException(nameof(recieverBotNames));
			}
			
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			if (!uint.TryParse(appIDAsText, out uint appID)) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(appIDAsText)));
			}

			if (!ulong.TryParse(contextIDAsText, out ulong contextID)) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(contextIDAsText)));
			}

			ItemIdentifier itemIdentifier;
			try {
				itemIdentifier = new ItemIdentifier(itemIdentifierAsText, marketable);
			} catch (Exception) {
				return FormatBotResponse(bot, String.Format(Strings.InvalidItemIdentifier, itemIdentifierAsText));
			}

			HashSet<Bot>? recieverBots = Bot.GetBots(recieverBotNames);
			if ((recieverBots == null) || (recieverBots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, recieverBotNames)) : null;
			}

			string[] amountStrings = amountsAsText.Split(",", StringSplitOptions.RemoveEmptyEntries);

			if (amountStrings.Length == 0) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsEmpty, nameof(amountStrings)));
			}

			if (amountStrings.Length == 1 && recieverBots.Count > 1) {
				amountStrings = Enumerable.Repeat(amountStrings[0], recieverBots.Count).ToArray();
			}
			
			if (amountStrings.Length != recieverBots.Count) {
				return FormatBotResponse(bot, String.Format(Strings.BotCountDoesNotEqualAmountCount, recieverBots.Count, amountStrings.Length));
			}

			List<uint> amounts = new List<uint>();
			int botIndex = 0;
			foreach (string amount in amountStrings) {
				if (!uint.TryParse(amount, out uint amountNum)) {
					if (itemIdentifier.ClassID == GemHandler.GemsClassID && (amount.ToUpperInvariant() == "QUEUE" || amount.ToUpperInvariant() == "Q")) {
						amountNum = BoosterHandler.BoosterHandlers[recieverBots.ElementAt(botIndex).BotName].GetGemsNeeded();
					} else {
						return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(amountNum)));
					}
				}

				amounts.Add(amountNum);
				botIndex++;
			}

			List<(Bot, uint)> recievers = Zip(recieverBots, amounts).ToList();

			return await InventoryHandler.SendItemToMultipleBots(bot, recievers, appID, contextID, itemIdentifier);
		}

		private static async Task<string?> ResponseSendItemToMultipleBots(EAccess access, ulong steamID, string senderBotName, string recieverBotNames, string amountsAsText, string appIDAsText, string contextIDAsText, string itemIdentifierAsText, bool? marketable = null) {
			if (String.IsNullOrEmpty(senderBotName)) {
				throw new ArgumentNullException(nameof(senderBotName));
			}

			Bot? sender = Bot.GetBot(senderBotName);

			if (sender == null) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, senderBotName)) : null;
			}

			return await ResponseSendItemToMultipleBots(sender, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(sender, access, steamID), recieverBotNames, amountsAsText, appIDAsText, contextIDAsText, itemIdentifierAsText, marketable).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseSendMultipleItemsToMultipleBots(Bot bot, EAccess access, string recieverBotNames, string amountsAsText, string appIDAsText, string contextIDAsText, string itemIdentifiersAsText, bool? marketable = null) {
			if (String.IsNullOrEmpty(appIDAsText)) {
				throw new ArgumentNullException(nameof(appIDAsText));
			}

			if (String.IsNullOrEmpty(contextIDAsText)) {
				throw new ArgumentNullException(nameof(contextIDAsText));
			}

			if (String.IsNullOrEmpty(amountsAsText)) {
				throw new ArgumentNullException(nameof(amountsAsText));
			}

			if (String.IsNullOrEmpty(itemIdentifiersAsText)) {
				throw new ArgumentNullException(nameof(itemIdentifiersAsText));
			}

			if (String.IsNullOrEmpty(recieverBotNames)) {
				throw new ArgumentNullException(nameof(recieverBotNames));
			}
			
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			if (!uint.TryParse(appIDAsText, out uint appID)) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(appIDAsText)));
			}

			if (!ulong.TryParse(contextIDAsText, out ulong contextID)) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsInvalid, nameof(contextIDAsText)));
			}

			HashSet<Bot>? recieverBots = Bot.GetBots(recieverBotNames);

			if ((recieverBots == null) || (recieverBots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, recieverBotNames)) : null;
			}

			List<string> itemIdentifierStrings = itemIdentifiersAsText.Split(ItemIdentifier.Delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			List<ItemIdentifier> itemIdentifiers = new List<ItemIdentifier>();
			foreach (string itemIdentifierString in itemIdentifierStrings) {
				try {
					itemIdentifiers.Add(new ItemIdentifier(itemIdentifierString, marketable));
				} catch (Exception) {
					return FormatBotResponse(bot, String.Format(Strings.InvalidItemIdentifier, itemIdentifierString));
				}
			}
			
			string[] amountStrings = amountsAsText.Split(",", StringSplitOptions.RemoveEmptyEntries);

			if (amountStrings.Length == 0) {
				return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorIsEmpty, nameof(amountStrings)));
			}

			if (amountStrings.Length == 1 && itemIdentifierStrings.Count > 1) {
				amountStrings = Enumerable.Repeat(amountStrings[0], itemIdentifierStrings.Count).ToArray();
			}
				
			if (amountStrings.Length != itemIdentifierStrings.Count) {
				return FormatBotResponse(bot, String.Format(Strings.ItemCountDoesNotEqualAmountCount, itemIdentifierStrings.Count, amountStrings.Length));
			}

			List<uint> amounts = new List<uint>();
			foreach (string amount in amountStrings) {
				if (!uint.TryParse(amount, out uint amountNum)) {
					return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(amountNum)));
				}

				amounts.Add(amountNum);
			}

			List<(ItemIdentifier, uint)> items = Zip(itemIdentifiers, amounts).ToList();

			return await InventoryHandler.SendMultipleItemsToMultipleBots(bot, recieverBots, appID, contextID, items).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseSendMultipleItemsToMultipleBots(EAccess access, ulong steamID, string senderBotName, string recieverBotNames, string amountsAsText, string appIDAsText, string contextIDAsText, string itemIdentifiersAsText, bool? marketable = null) {
			if (String.IsNullOrEmpty(senderBotName)) {
				throw new ArgumentNullException(nameof(senderBotName));
			}

			Bot? sender = Bot.GetBot(senderBotName);

			if (sender == null) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, senderBotName)) : null;
			}

			return await ResponseSendMultipleItemsToMultipleBots(sender, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(sender, access, steamID), recieverBotNames, amountsAsText, appIDAsText, contextIDAsText, itemIdentifiersAsText, marketable).ConfigureAwait(false);
		}

		private static string? ResponseTradeCheck(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			MethodInfo? OnTradeCheckTimer = typeof(Bot).GetMethods(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).FirstOrDefault(x => x.Name == "OnTradeCheckTimer");

			if (OnTradeCheckTimer == null) {
				return FormatBotResponse(bot, Strings.PluginError);
			}

			OnTradeCheckTimer.Invoke(bot, new object[] { Type.Missing });

			return FormatBotResponse(bot, Strings.HandlingIncomingTrades);
		}

		private static string? ResponseTradeCheck(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
			}

			IEnumerable<string?> results = bots.Select(bot => ResponseTradeCheck(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID)));

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseTradeCount(Bot bot, EAccess access, string? fromBotNamesAndIDs = null) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			HashSet<TradeOffer>? tradeOffers = await bot.ArchiWebHandler.GetTradeOffers(true, true, false, true).ConfigureAwait(false);

			if (tradeOffers == null) {
				return FormatBotResponse(bot, Strings.IncomingTradeFetchFailed);
			}

			if (fromBotNamesAndIDs != null) {
				string[] botNamesAndIDs = fromBotNamesAndIDs.Split(",", StringSplitOptions.RemoveEmptyEntries);
				List<ulong> steamIDs = new();

				foreach (string botNameOrID in botNamesAndIDs) {
					HashSet<Bot>? bots = Bot.GetBots(botNameOrID);
					if (bots != null && bots.Count != 0) {
						foreach(Bot b in bots) {
							steamIDs.Add(b.SteamID);
						}
					} else if (ulong.TryParse(botNameOrID, out ulong steamID)) {
						steamIDs.Add(steamID);
					} else {
						return FormatBotResponse(bot, String.Format(Strings.IncomingTradeFromInvalidUser, botNameOrID));
					}
				}

				return FormatBotResponse(bot, String.Format(Strings.IncomingTradeCountFromUser, tradeOffers.Where(offer => steamIDs.Contains(offer.OtherSteamID64)).Count(), fromBotNamesAndIDs));
			}

			return FormatBotResponse(bot, String.Format(Strings.IncomingTradeCount, tradeOffers.Count));
		}

		private static async Task<string?> ResponseTradeCount(EAccess access, ulong steamID, string botNames, string? fromBotNamesAndIDs = null) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
			}

			IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseTradeCount(bot, ArchiSteamFarm.Steam.Interaction.Commands.GetProxyAccess(bot, access, steamID), fromBotNamesAndIDs))).ConfigureAwait(false);

			List<string?> responses = new(results.Where(result => !String.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}

		private static async Task<string?> ResponseUnpackGems(Bot bot, EAccess access) {
			if (access < EAccess.Master) {
				return null;
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			return await GemHandler.UnpackGems(bot).ConfigureAwait(false);
		}

		private static async Task<string?> ResponseUnpackGems(EAccess access, ulong steamID, string botNames) {
			if (String.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);

			if ((bots == null) || (bots.Count == 0)) {
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
				return FormatBotResponse(bot, ArchiSteamFarm.Localization.Strings.BotNotConnected);
			}

			uint subtractFrom = 0;
			if (subtractFromAsText != null) {
				if (!uint.TryParse(subtractFromAsText, out subtractFrom)) {
					return FormatBotResponse(bot, String.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(subtractFrom)));
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
				return access >= EAccess.Owner ? FormatStaticResponse(String.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)) : null;
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
