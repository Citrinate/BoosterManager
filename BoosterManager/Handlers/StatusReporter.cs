using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using SteamKit2;

// For when long-running commands are issued through Steam chat, this is used to send status reports from the bot the command was sent to, to the user who issued the command
// If the commands weren't issued through Steam chat, just log the status reports

namespace BoosterManager {
	internal sealed class StatusReporter {
		private Bot? Sender; // When we send status alerts, they'll come from this bot
		private ulong RecipientSteamID; // When we send status alerts, they'll go to this SteamID
		private ConcurrentDictionary<Bot, List<string>> Reports = new();
		private ConcurrentDictionary<Bot, List<string>> PreviousReports = new();
		private const uint ReportDelaySeconds = 5;

		private Timer ReportTimer;

		internal StatusReporter(Bot? sender = null, ulong recipientSteamID = 0) {
			Sender = sender;
			RecipientSteamID = recipientSteamID;
			ReportTimer = new Timer(async _ => await Send().ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite);
		}

		internal void Report(Bot reportingBot, string report, bool suppressDuplicateMessages = false) {
			if (suppressDuplicateMessages) {
				bool existsInReports = Reports.TryGetValue(reportingBot, out var reports) && reports.Contains(report);
				bool existsInPreviousReports = PreviousReports.TryGetValue(reportingBot, out var previousReports) && previousReports.Contains(report);

				if (existsInReports || existsInPreviousReports) {
					return;
				}
			}

			Reports.AddOrUpdate(reportingBot, new List<string>() { report }, (_, reports) => { reports.Add(report); return reports; });

			// I prefer to send all reports in as few messages as possible
			// As long as reports continue to come in, we wait
			ReportTimer.Change(TimeSpan.FromSeconds(ReportDelaySeconds), Timeout.InfiniteTimeSpan);
		}

		private async Task Send() {
			if (Sender != null && !Sender.IsConnectedAndLoggedOn) {
				ReportTimer.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);

				return;
			}

			List<string> messages = new List<string>();
			List<Bot> bots = Reports.Keys.OrderBy(bot => bot.BotName).ToList();

			foreach (Bot bot in bots) {
				messages.Add(Commands.FormatBotResponse(bot, String.Join(Environment.NewLine, Reports[bot])));
				if (Reports[bot].Count > 1) {
					// Add an extra line if there's more than 1 message from a bot
					messages.Add("");
				}

				if (Reports.TryRemove(bot, out List<string>? previousReports)) {
					if (previousReports != null) {
						PreviousReports.AddOrUpdate(bot, previousReports, (_, _) => previousReports);
					}
				}
			}

			if (Sender == null || RecipientSteamID == 0 || !new SteamID(RecipientSteamID).IsIndividualAccount || Sender.SteamFriends.GetFriendRelationship(RecipientSteamID) != EFriendRelationship.Friend) {
				// Command was sent outside of Steam chat, or can't send a Steam message
				ASF.ArchiLogger.LogGenericInfo(String.Join(Environment.NewLine, messages));
			} else {
				await Sender.SendMessage(RecipientSteamID, String.Join(Environment.NewLine, messages)).ConfigureAwait(false);
			}
		}
	}
}
