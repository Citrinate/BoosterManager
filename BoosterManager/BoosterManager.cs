using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Plugins.Interfaces;
using Newtonsoft.Json.Linq;

namespace BoosterManager {
	[Export(typeof(IPlugin))]
	public sealed class BoosterManager : IASF, IBotModules, IBotCommand2 {
		public string Name => nameof(BoosterManager);
		public Version Version => typeof(BoosterManager).Assembly.GetName().Version ?? new Version("0");

		public Task OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo("BoosterManager ASF Plugin by Citrinate");
			return Task.CompletedTask;
		}

		public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) => await Commands.Response(bot, access, steamID, message, args).ConfigureAwait(false);

		public Task OnASFInit(IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null) {
			if (additionalConfigProperties == null) {
				return Task.FromResult(0);
			}

			foreach (KeyValuePair<string, JToken> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "AllowCraftUntradableBoosters" when configProperty.Value.Type == JTokenType.Boolean: {
						ASF.ArchiLogger.LogGenericInfo("Allow Craft Untradable Boosters : " + configProperty.Value);
						BoosterHandler.AllowCraftUntradableBoosters = configProperty.Value.ToObject<bool>();
						break;
					}
					case "BoosterDelayBetweenBots" when configProperty.Value.Type == JTokenType.Integer: {
						ASF.ArchiLogger.LogGenericInfo("Booster Delay Between Bots : " + configProperty.Value);
						BoosterHandler.UpdateBotDelays((int)configProperty.Value.ToObject<uint>());
						break;
					}
					case "BoosterDataAPI" when configProperty.Value.Type == JTokenType.String: {
						ASF.ArchiLogger.LogGenericInfo("Booster Data API : " + configProperty.Value);
						DataHandler.BoosterDataAPI = new Uri(configProperty.Value.ToObject<string>()!);
						break;
					}
					case "MarketListingsAPI" when configProperty.Value.Type == JTokenType.String: {
						ASF.ArchiLogger.LogGenericInfo("Market Listings API : " + configProperty.Value);
						DataHandler.MarketListingsAPI = new Uri(configProperty.Value.ToObject<string>()!);
						break;
					}
					case "MarketHistoryAPI" when configProperty.Value.Type == JTokenType.String: {
						ASF.ArchiLogger.LogGenericInfo("Market History API : " + configProperty.Value);
						DataHandler.MarketHistoryAPI = new Uri(configProperty.Value.ToObject<string>()!);
						break;
					}
					case "MarketHistoryDelay" when configProperty.Value.Type == JTokenType.Integer: {
						ASF.ArchiLogger.LogGenericInfo("Market History Delay : " + configProperty.Value);
						DataHandler.MarketHistoryDelay = configProperty.Value.ToObject<uint>();
						break;
					}
				}
			}

			return Task.FromResult(0);
		}

		public Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null) {
			BoosterHandler.AddHandler(bot);

			if (additionalConfigProperties == null) {
				return Task.FromResult(0);
			}

			foreach (KeyValuePair<string, JToken> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "GamesToBooster" when configProperty.Value.Type == JTokenType.Array && configProperty.Value.Any(): {
						bot.ArchiLogger.LogGenericInfo("Games To Booster : " + string.Join(",", configProperty.Value));
						HashSet<uint>? gameIDs = configProperty.Value.ToObject<HashSet<uint>>();
						if (gameIDs == null) {
							bot.ArchiLogger.LogNullError(gameIDs);
						} else {
							BoosterHandler.BoosterHandlers[bot.BotName].SchedulePermanentBoosters(gameIDs);
						}
						break;
					}
				}
			}

			return Task.FromResult(0);
		}
	}
}
