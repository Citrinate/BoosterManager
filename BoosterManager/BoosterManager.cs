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

		public async Task OnASFInit(IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null) {
			if (additionalConfigProperties == null) {
				return;
			}

			foreach (KeyValuePair<string, JToken> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "BoosterDelayBetweenBots" when configProperty.Value.Type == JTokenType.Integer: {
						ASF.ArchiLogger.LogGenericInfo("Booster Delay Between Bots : " + configProperty.Value);
						await Task.Run(() => BoosterHandler.UpdateBotDelays(configProperty.Value.ToObject<int>())).ConfigureAwait(false);
						break;
					}
				}
			}
		}

		public async Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null) {
			BoosterHandler.AddHandler(bot);

			if (additionalConfigProperties == null) {
				return;
			}

			foreach (KeyValuePair<string, JToken> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "GamesToBooster" when configProperty.Value.Type == JTokenType.Array && configProperty.Value.Any(): {
						bot.ArchiLogger.LogGenericInfo("Games To Booster : " + string.Join(",", configProperty.Value));
						HashSet<uint>? gameIDs = configProperty.Value.ToObject<HashSet<uint>>();
						if (gameIDs == null) {
							bot.ArchiLogger.LogNullError(gameIDs);
						} else {
							await Task.Run(() => BoosterHandler.BoosterHandlers[bot.BotName].SchedulePermanentBoosters(gameIDs)).ConfigureAwait(false);
						}
						break;
					}
				}
			}
		}
	}
}
