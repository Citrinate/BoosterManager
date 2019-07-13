using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Plugins;
using Newtonsoft.Json.Linq;

namespace BoosterCreator {
	[Export(typeof(IPlugin))]
	public sealed class BoosterCreator : IBotModules, IBotCommand {
		public string Name => nameof(BoosterCreator);
		public Version Version => typeof(BoosterCreator).Assembly.GetName().Version;

		public void OnLoaded() => ASF.ArchiLogger.LogGenericInfo("BoosterCreator ASF Plugin by Out (https://steamcommunity.com/id/outzzz) | fork by Ryzhehvost");

		public async Task<string> OnBotCommand(Bot bot, ulong steamID, string message, string[] args) => await Commands.Response(bot, steamID, message, args).ConfigureAwait(false);

		public async void OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JToken> additionalConfigProperties = null) {
			if (additionalConfigProperties == null) {
				return;
			}
			
			foreach (KeyValuePair<string, JToken> configProperty in additionalConfigProperties) {
				switch (configProperty.Key) {
					case "GamesToBooster" when configProperty.Value.Type == JTokenType.Array && configProperty.Value.Any(): {
						if (BoosterHandler.BoosterHandlers.ContainsKey(bot.BotName)) {
							BoosterHandler.BoosterHandlers[bot.BotName].Dispose();
							BoosterHandler.BoosterHandlers[bot.BotName] = null;
						}

						bot.ArchiLogger.LogGenericInfo("GamesToBooster : " + string.Join(",", configProperty.Value));
						await Task.Run(() => BoosterHandler.BoosterHandlers[bot.BotName] = new BoosterHandler(bot, configProperty.Value.ToObject<HashSet<uint>>())).ConfigureAwait(false);
						break;
					}
				}
			}
		}
	}
}
