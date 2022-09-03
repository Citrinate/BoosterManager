using ArchiSteamFarm.Steam;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoosterManager {
	internal sealed class BoosterHandler : IDisposable {
		private readonly Bot Bot;
		private readonly BoosterQueue BoosterQueue;
		private Bot RespondingBot; // When we send status alerts, they'll come from this bot
		private ulong RecipientSteamID; // When we send status alerts, they'll go to this SteamID
		internal static ConcurrentDictionary<string, Timer> ResponseTimers = new();
		internal HashSet<string> StoredResponses = new();
		private string LastResponse = "";
		internal static ConcurrentDictionary<string, BoosterHandler> BoosterHandlers = new();
		private static int DelayBetweenBots = 0; // Delay, in minutes, between when bots will craft boosters

		internal BoosterHandler(Bot bot) {
			Bot = bot ?? throw new ArgumentNullException(nameof(bot));
			BoosterQueue = new BoosterQueue(Bot, this);
			RespondingBot = bot;
			RecipientSteamID = Bot.Actions.GetFirstSteamMasterID();
		}

		public void Dispose() {
			BoosterQueue.Dispose();
		}

		internal static void AddHandler(Bot bot) {
			if (BoosterHandlers.ContainsKey(bot.BotName)) {
				BoosterHandlers[bot.BotName].Dispose();
				BoosterHandlers.TryRemove(bot.BotName, out BoosterHandler _);
			}

			if (BoosterHandlers.TryAdd(bot.BotName, new BoosterHandler(bot))) {
				UpdateBotDelays();
			}
		}

		internal static void UpdateBotDelays(int? delayInSeconds = null) {
			if(DelayBetweenBots == 0 && (delayInSeconds == null || delayInSeconds == 0)) {
				return;
			}

			DelayBetweenBots = delayInSeconds ?? DelayBetweenBots;
			List<string> botNames = BoosterHandlers.Keys.ToList<string>();
			botNames.Sort();
			foreach (KeyValuePair<string, BoosterHandler> kvp in BoosterHandlers) {
				// TODO: Need to figure out a better way to implement these kinds of delays
				// Because of the 24 hour cooldown, any delay will carry over from one day to the next. With a delay of 1 minute, on day one we'll correctly delay for 1 minute.
				// If two bots were going to craft a booster at say, 12:00; one will craft at 12:00 and the next at 12:01. On day two however, we'll add another 1 minute delay.
				// The first bot will still craft at 12:00, but the second will now craft at 12:02, and the difference between them will keep drifting apart.
				int index = botNames.IndexOf(kvp.Key);
				kvp.Value.BoosterQueue.BoosterDelay = DelayBetweenBots * index;
			}
		}

		internal string ScheduleBoosters(HashSet<uint> gameIDs, Bot respondingBot, ulong recipientSteamID) {
			RespondingBot = respondingBot;
			RecipientSteamID = recipientSteamID;
			foreach (uint gameID in gameIDs) {
				BoosterQueue.AddBooster(gameID, BoosterType.OneTime);
			}
			BoosterQueue.OnBoosterInfosUpdated -= ScheduleBoostersResponse;
			BoosterQueue.OnBoosterInfosUpdated += ScheduleBoostersResponse;
			BoosterQueue.Start();

			return Commands.FormatBotResponse(Bot, String.Format("Attempting to craft {0} boosters...", gameIDs.Count));
		}

		private void ScheduleBoostersResponse() {
			BoosterQueue.OnBoosterInfosUpdated -= ScheduleBoostersResponse;
			string? message = BoosterQueue.GetShortStatus();
			if (message == null) {
				PerpareStatusReport("Bot cannot craft boosters for the requested games");

				return;
			}

			PerpareStatusReport(message);
		}

		internal void SchedulePermanentBoosters(HashSet<uint> gameIDs) {
			foreach (uint gameID in gameIDs) {
				BoosterQueue.AddBooster(gameID, BoosterType.Permanent);
			}
			BoosterQueue.Start();
		}

		internal string UnscheduleBoosters(HashSet<uint>? gameIDs = null, int? timeLimitHours = null) {
			HashSet<uint> removedGameIDs = BoosterQueue.RemoveBoosters(gameIDs, timeLimitHours);

			if (removedGameIDs.Count == 0) {
				if (timeLimitHours == null) {
					return Commands.FormatBotResponse(Bot, "Bot was not attempting to craft any of those boosters.  If you're trying to remove boosters from your \"GamesToBooster\" config setting, you'll need to remove them from your config file.");
				}
				
				return Commands.FormatBotResponse(Bot, "Didn't find any boosters that could be removed.");

			}

			return Commands.FormatBotResponse(Bot, String.Format("Will no longer craft these {0} boosters: {1}", removedGameIDs.Count, String.Join(", ", removedGameIDs)));
		}

		internal string GetStatus() {
			return Commands.FormatBotResponse(Bot, BoosterQueue.GetStatus());
		}

		internal void PerpareStatusReport(string message, bool suppressDuplicateMessages = false) {
			if (suppressDuplicateMessages && LastResponse == message) {
				return;
			}

			LastResponse = message;
			// Could be that multiple bots will try to respond all at once individually.  Start a timer, during which all messages will be logged and sent all together when the timer triggers.
			if (StoredResponses.Count == 0) {
				message = Commands.FormatBotResponse(Bot, message);
			}
			StoredResponses.Add(message);
			if (!ResponseTimers.ContainsKey(RespondingBot.BotName)) {
				ResponseTimers[RespondingBot.BotName] = new Timer(
					async e => await SendStatusReport(RespondingBot, RecipientSteamID).ConfigureAwait(false),
					null,
					GetMillisecondsFromNow(DateTime.Now.AddSeconds(5)),
					Timeout.Infinite
				);
			}
		}

		private static async Task SendStatusReport(Bot respondingBot, ulong recipientSteamID) {
			if (!respondingBot.IsConnectedAndLoggedOn) {
				ResponseTimers[respondingBot.BotName].Change(BoosterHandler.GetMillisecondsFromNow(DateTime.Now.AddSeconds(1)), Timeout.Infinite);

				return;
			}

			ResponseTimers.TryRemove(respondingBot.BotName, out Timer? _);
			HashSet<string> messages = new HashSet<string>();
			List<string> botNames = BoosterHandlers.Keys.ToList<string>();
			botNames.Sort();
			foreach (string botName in botNames) {
				if (BoosterHandlers[botName].StoredResponses.Count == 0 
					|| BoosterHandlers[botName].RespondingBot.BotName != respondingBot.BotName) {
					continue;
				}

				messages.Add(String.Join(Environment.NewLine, BoosterHandlers[botName].StoredResponses));
				BoosterHandlers[botName].StoredResponses.Clear();
			}
			
			string message = String.Join(Environment.NewLine, messages);
			await respondingBot.SendMessage(recipientSteamID, message).ConfigureAwait(false);
		}

		private static int GetMillisecondsFromNow(DateTime then) => Math.Max(0, (int) (then - DateTime.Now).TotalMilliseconds);
	}
}
