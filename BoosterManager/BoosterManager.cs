﻿using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam.Exchange;
using System.Text.Json;
using ArchiSteamFarm.Helpers.Json;
using SteamKit2;

namespace BoosterManager {
	[Export(typeof(IPlugin))]
	public sealed class BoosterManager : IASF, IBotModules, IBotCommand2, IBotTradeOfferResults, IGitHubPluginUpdates, IBotConnection {
		public string Name => nameof(BoosterManager);
		public string RepositoryName => "Citrinate/BoosterManager";
		public Version Version => typeof(BoosterManager).Assembly.GetName().Version ?? new Version("0");
		internal static GlobalCache? GlobalCache;

		public Task OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo("BoosterManager ASF Plugin by Citrinate");

			return Task.CompletedTask;
		}

		public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
			return await Commands.Response(bot, access, steamID, message, args).ConfigureAwait(false);
		}

		public async Task OnASFInit(IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
			if (GlobalCache == null) {
				GlobalCache = await GlobalCache.CreateOrLoad().ConfigureAwait(false);
			}

			MarketHandler.StartMarketAlertTimer();

			if (additionalConfigProperties == null) {
				return;
			}

			foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "AllowCraftUntradableBoosters" when (configProperty.Value.ValueKind == JsonValueKind.True || configProperty.Value.ValueKind == JsonValueKind.False): {
						ASF.ArchiLogger.LogGenericInfo("Allow Craft Untradable Boosters : " + configProperty.Value);
						BoosterHandler.AllowCraftUntradableBoosters = configProperty.Value.GetBoolean();
						break;
					}
					case "AllowCraftUnmarketableBoosters" when (configProperty.Value.ValueKind == JsonValueKind.True || configProperty.Value.ValueKind == JsonValueKind.False): {
						ASF.ArchiLogger.LogGenericInfo("Allow Craft Unmarketable Boosters : " + configProperty.Value);
						BoosterHandler.AllowCraftUnmarketableBoosters = configProperty.Value.GetBoolean();
						break;
					}
					case "BoosterDataAPI" when configProperty.Value.ValueKind == JsonValueKind.String: {
						ASF.ArchiLogger.LogGenericInfo("Booster Data API : " + configProperty.Value);
						DataHandler.BoosterDataAPI = new Uri(configProperty.Value.GetString()!);
						break;
					}
					case "InventoryHistoryAPI" when configProperty.Value.ValueKind == JsonValueKind.String: {
						ASF.ArchiLogger.LogGenericInfo("Inventory History API : " + configProperty.Value);
						DataHandler.InventoryHistoryAPI = new Uri(configProperty.Value.GetString()!);
						break;
					}
					case "MarketListingsAPI" when configProperty.Value.ValueKind == JsonValueKind.String: {
						ASF.ArchiLogger.LogGenericInfo("Market Listings API : " + configProperty.Value);
						DataHandler.MarketListingsAPI = new Uri(configProperty.Value.GetString()!);
						break;
					}
					case "MarketHistoryAPI" when configProperty.Value.ValueKind == JsonValueKind.String: {
						ASF.ArchiLogger.LogGenericInfo("Market History API : " + configProperty.Value);
						DataHandler.MarketHistoryAPI = new Uri(configProperty.Value.GetString()!);
						break;
					}
					case "LogDataPageDelay" or "MarketHistoryDelay" when configProperty.Value.ValueKind == JsonValueKind.Number: {
						ASF.ArchiLogger.LogGenericInfo("Log Data Page Delay : " + configProperty.Value);
						DataHandler.LogDataPageDelay = configProperty.Value.ToJsonObject<uint>();
						break;
					}
					case "InventoryHistoryAppFilter" when configProperty.Value.ValueKind == JsonValueKind.Array && configProperty.Value.GetArrayLength() > 0: {
						ASF.ArchiLogger.LogGenericInfo("Inventory History App Filter : " + string.Join(",", configProperty.Value));
						List<uint>? appIDs = configProperty.Value.ToJsonObject<List<uint>>();
						if (appIDs == null) {
							ASF.ArchiLogger.LogNullError(appIDs);
						} else {
							DataHandler.InventoryHistoryAppFilter = appIDs;
						}
						break;
					}
				}
			}

			return;
		}

		public Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
			string databaseFilePath = Bot.GetFilePath(String.Format("{0}_{1}", bot.BotName, nameof(BoosterManager)), Bot.EFileType.Database);
			BoosterHandler.AddHandler(bot, BoosterDatabase.CreateOrLoad(databaseFilePath));

			if (additionalConfigProperties == null) {
				return Task.FromResult(0);
			}

			foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "GamesToBooster" when configProperty.Value.ValueKind == JsonValueKind.Array && configProperty.Value.GetArrayLength() > 0: {
						bot.ArchiLogger.LogGenericInfo("Games To Booster : " + string.Join(",", configProperty.Value));
						HashSet<uint>? gameIDs = configProperty.Value.ToJsonObject<HashSet<uint>>();
						if (gameIDs == null) {
							bot.ArchiLogger.LogNullError(gameIDs);
						} else {
							BoosterHandler.BoosterHandlers[bot.BotName].ScheduleBoosters(BoosterJobType.Permanent, gameIDs.ToList(), StatusReporter.StatusLogger());
						}
						break;
					}
				}
			}

			return Task.FromResult(0);
		}

		public Task OnBotTradeOfferResults(Bot bot, IReadOnlyCollection<ParseTradeResult> tradeResults) {
			ArgumentNullException.ThrowIfNull(bot);

			if ((tradeResults == null) || (tradeResults.Count == 0)) {
				throw new ArgumentNullException(nameof(tradeResults));
			}

			// Only trigger when recieving gems
			if (!tradeResults.Any(
				tradeResult => tradeResult is { Result: ParseTradeResult.EResult.Accepted, Confirmed: true } 
					&& tradeResult.ItemsToReceive?.Any(item => ItemIdentifier.GemIdentifier.IsItemMatch(item)) == true
				)
			) {
				return Task.CompletedTask;
			}

			BoosterHandler.BoosterHandlers[bot.BotName].OnGemsRecieved();

			return Task.CompletedTask;
		}
		
		public Task OnBotLoggedOn(Bot bot) {
			BoosterHandler.BoosterHandlers[bot.BotName].OnBotLoggedOn();

			return Task.CompletedTask;
		}

		public Task OnBotDisconnected(Bot bot, EResult reason) {
			return Task.FromResult(0);
		}
	}
}
