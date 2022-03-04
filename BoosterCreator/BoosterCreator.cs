using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Plugins.Interfaces;
using Newtonsoft.Json.Linq;

namespace BoosterCreator {
	[Export(typeof(IPlugin))]
	public sealed class BoosterCreator : IBotModules, IBotCommand2 {
		public string Name => nameof(BoosterCreator);
		public Version Version => typeof(BoosterCreator).Assembly.GetName().Version ?? new Version("0");

		public Task OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo("BoosterCreator ASF Plugin by Out (https://steamcommunity.com/id/outzzz) | fork by Ryzhehvost");
			return Task.CompletedTask;
		}

		public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) => await Commands.Response(bot, steamID, message, args).ConfigureAwait(false);

		public async Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null) {
			if (additionalConfigProperties == null) {
				return;
			}

			foreach (KeyValuePair<string, JToken> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "GamesToBooster" when configProperty.Value.Type == JTokenType.Array && configProperty.Value.Any(): {
						if (BoosterHandler.BoosterHandlers.ContainsKey(bot.BotName)&&(BoosterHandler.BoosterHandlers[bot.BotName] != null)) {
							BoosterHandler.BoosterHandlers[bot.BotName]!.Dispose();
							BoosterHandler.BoosterHandlers[bot.BotName] = null;
						}

						bot.ArchiLogger.LogGenericInfo("Games To Booster : " + string.Join(",", configProperty.Value));
						IReadOnlyCollection<uint>? gameIDs = configProperty.Value.ToObject<HashSet<uint>>();
						if (gameIDs == null) {
							bot.ArchiLogger.LogNullError(nameof(gameIDs));
						} else {
							await Task.Run(() => BoosterHandler.BoosterHandlers[bot.BotName] = new BoosterHandler(bot, gameIDs)).ConfigureAwait(false);
						}
						break;
					}
				}
			}
		}
	}
}
