using ArchiSteamFarm.Collections;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BoosterManager {
	internal sealed class BoosterHandler : IDisposable {
		private readonly Bot Bot;
		internal BoosterDatabase BoosterDatabase { get; private set; }
		internal readonly BoosterQueue BoosterQueue;
		internal static ConcurrentDictionary<string, BoosterHandler> BoosterHandlers = new();
		internal ConcurrentList<BoosterJob> Jobs = new();
		internal static bool AllowCraftUntradableBoosters = true;
		internal static bool AllowCraftUnmarketableBoosters = true;

		private BoosterHandler(Bot bot, BoosterDatabase boosterDatabase) {
			ArgumentNullException.ThrowIfNull(boosterDatabase);

			Bot = bot;
			BoosterDatabase = boosterDatabase;
			BoosterQueue = new BoosterQueue(Bot);
		}

		public void Dispose() {
			BoosterQueue.Dispose();
		}

		internal static void AddHandler(Bot bot, BoosterDatabase boosterDatabase) {
			if (BoosterHandlers.ContainsKey(bot.BotName)) {
				BoosterHandlers[bot.BotName].CancelBoosterJobs();
				BoosterHandlers[bot.BotName].Dispose();
				BoosterHandlers.TryRemove(bot.BotName, out BoosterHandler? _);
			}

			BoosterHandler handler = new BoosterHandler(bot, boosterDatabase);
			if (BoosterHandlers.TryAdd(bot.BotName, handler)) {
				handler.RestoreBoosterJobs();
			}
		}

		internal string ScheduleBoosters(BoosterJobType jobType, List<uint> gameIDs, StatusReporter craftingReporter) {
			Jobs.Add(new BoosterJob(Bot, jobType, gameIDs, craftingReporter));

			return Commands.FormatBotResponse(Bot, String.Format(Strings.BoosterCreationStarting, gameIDs.Count));
		}

		internal static void SmartScheduleBoosters(BoosterJobType jobType, HashSet<Bot> bots, Dictionary<Bot, Dictionary<uint, Steam.BoosterInfo>> botBoosterInfos, List<(uint gameID, uint amount)> gameIDsWithAmounts, StatusReporter craftingReporter) {
			// Group together any duplicate gameIDs
			gameIDsWithAmounts = gameIDsWithAmounts.GroupBy(item => item.gameID).Select(group => (group.First().gameID, (uint) group.Sum(item => item.amount))).ToList();

			// Figure out the most efficient way to queue the given boosters and amounts using the given bots
			Dictionary<Bot, List<uint>> gameIDsToQueue = new();
			DateTime now = DateTime.Now;

			foreach (var gameIDWithAmount in gameIDsWithAmounts) {
				uint gameID = gameIDWithAmount.gameID;
				uint amount = gameIDWithAmount.amount;

				// Get all the data we need to determine which is the best bot for this booster
				Dictionary<Bot, (int numQueued, DateTime nextCraftTime)> botStates = new();
				foreach(Bot bot in bots) {
					if (!botBoosterInfos.TryGetValue(bot, out Dictionary<uint, Steam.BoosterInfo>? boosterInfos)) {
						continue;
					}

					if (!boosterInfos.TryGetValue(gameID, out Steam.BoosterInfo? boosterInfo)) {
						continue;
					}

					botStates.Add(bot, (BoosterHandlers[bot.BotName].Jobs.GetNumBoosters(gameID), boosterInfo.AvailableAtTime ?? now));
				}

				// No bots can craft boosters for this gameID
				if (botStates.Count == 0) {
					continue;
				}

				for (int i = 0; i < amount; i++) {
					// Find the best bot for this booster
					Bot? bestBot = null;
					foreach(var botState in botStates) {
						if (bestBot == null) {
							bestBot = botState.Key;

							continue;
						}

						Bot bot = botState.Key;
						int numQueued = botState.Value.numQueued;
						DateTime nextCraftTime = botState.Value.nextCraftTime;

						if (botStates[bestBot].nextCraftTime.AddDays(botStates[bestBot].numQueued) > nextCraftTime.AddDays(numQueued)) {
							bestBot = bot;
						}
					}

					if (bestBot == null) {
						break;
					}

					// Assign the booster to the best bot
					gameIDsToQueue.TryAdd(bestBot, new List<uint>());
					gameIDsToQueue[bestBot].Add(gameID);
					var bestBotState = botStates[bestBot];
					botStates[bestBot] = (bestBotState.numQueued + 1, bestBotState.nextCraftTime);
				}
			}

			if (gameIDsToQueue.Count == 0) {
				foreach(Bot bot in bots) {
					craftingReporter.Report(bot, Strings.BoostersUncraftable);
				}

				craftingReporter.ForceSend();

				return;
			}

			// Queue the boosters
			foreach (var item in gameIDsToQueue) {
				Bot bot = item.Key;
				List<uint> gameIDs = item.Value;

				BoosterHandlers[bot.BotName].Jobs.Add(new BoosterJob(bot, jobType, gameIDs, craftingReporter));
				craftingReporter.Report(bot, String.Format(Strings.BoosterCreationStarting, gameIDs.Count));
			}

			craftingReporter.ForceSend();
		}

		internal string UnscheduleBoosters(HashSet<uint>? gameIDs = null, int? timeLimitHours = null) {
			List<uint> removedGameIDs = new List<uint>();

			if (timeLimitHours == 0) {
				// Cancel everything, as everything takes more than 0 hours to craft
				removedGameIDs.AddRange(Jobs.RemoveAllBoosters());
			} else {
				// Cancel all boosters for a certain game
				if (gameIDs != null) {
					foreach(uint gameID in gameIDs) {
						int numRemoved = Jobs.RemoveBoosters(gameID);

						if (numRemoved > 0) {
							for (int i = 0; i < numRemoved; i++) {
								removedGameIDs.Add(gameID);
							}
						}
					}
				}

				// Cancel all boosters that will take more than a certain number of hours to craft
				if (timeLimitHours != null) {
					DateTime timeLimit = DateTime.Now.AddHours(timeLimitHours.Value);
					(List<Booster> queuedBoosters, List<uint> unqueuedBoosters) = Jobs.QueuedAndUnqueuedBoosters();

					foreach (Booster booster in queuedBoosters) {
						int unqueuedCount = unqueuedBoosters.Where(x => x == booster.GameID).Count();
						DateTime boosterCraftTime = booster.GetAvailableAtTime();

						if (unqueuedCount > 0) {
							for (int i = 0; i < unqueuedCount; i++) {
								if (boosterCraftTime.AddDays(i + 1) > timeLimit) {
									if (Jobs.RemoveUnqueuedBooster(booster.GameID)) {
										removedGameIDs.Add(booster.GameID);
									}
								}
							}
						}

						if (boosterCraftTime > timeLimit) {
							if (Jobs.RemoveQueuedBooster(booster.GameID)) {
								removedGameIDs.Add(booster.GameID);
							}
						}
					}
				}
			}

			if (removedGameIDs.Count == 0) {
				if (timeLimitHours != null) {
					return Commands.FormatBotResponse(Bot, Strings.QueueRemovalByTimeFail);
				} else {
					return Commands.FormatBotResponse(Bot, Strings.QueueRemovalByAppFail);
				}
			}

			IEnumerable<string> gameIDStringsWithMultiples = removedGameIDs.GroupBy(x => x).Select(group => group.Count() == 1 ? group.Key.ToString() : String.Format("{0} (x{1})", group.Key, group.Count()));

			return Commands.FormatBotResponse(Bot, String.Format(Strings.QueueRemovalSuccess, removedGameIDs.Count, String.Join(", ", gameIDStringsWithMultiples)));
		}

		internal void UpdateBoosterJobs() {
			Jobs.Finished().ToList().ForEach(job => {
				job.Finish();
				Jobs.Remove(job);
			});

			BoosterDatabase.UpdateBoosterJobs(Jobs.Limited().Unfinised().SaveState());
		}

		private void CancelBoosterJobs() {
			foreach (BoosterJob job in Jobs) {
				job.Stop();
			}

			Jobs.Clear();
		}

		private void RestoreBoosterJobs() {
			uint? craftingGameID = BoosterDatabase.CraftingGameID;
			DateTime? craftingTime = BoosterDatabase.CraftingTime;
			if (craftingGameID != null && craftingTime != null) {
				// We were in the middle of crafting a booster when ASF was reset, check to see if that booster was crafted or not
				void handler(Dictionary<uint, Steam.BoosterInfo> boosterInfos) {
					try {
						if (!boosterInfos.TryGetValue(craftingGameID.Value, out Steam.BoosterInfo? newBoosterInfo)) {
							// No longer have access to craft boosters for this game (game removed from account, or sometimes due to very rare Steam bugs)

							return;
						}

						if (newBoosterInfo.Unavailable && newBoosterInfo.AvailableAtTime != null
							&& newBoosterInfo.AvailableAtTime != craftingTime.Value
							&& (newBoosterInfo.AvailableAtTime.Value - craftingTime.Value).TotalHours > 2 // Make sure the change in time isn't due to daylight savings
						) {
							// Booster was crafted
							Bot.ArchiLogger.LogGenericInfo(String.Format(Strings.BoosterUnexpectedlyCrafted, craftingGameID.Value));

							// Remove 1 of this booster from our jobs
							BoosterDatabase.BoosterJobs.Any(jobState => jobState.GameIDs.Remove(craftingGameID.Value));
							BoosterDatabase.PostCraft();

							foreach (BoosterJobState jobState in BoosterDatabase.BoosterJobs) {
								Jobs.Add(new BoosterJob(Bot, BoosterJobType.Limited, jobState));
							}
						}
					} finally {
						BoosterQueue.OnBoosterInfosUpdated -= handler;
					}
				}

				BoosterQueue.OnBoosterInfosUpdated += handler;
				BoosterQueue.Start();
			} else {
				foreach (BoosterJobState jobState in BoosterDatabase.BoosterJobs) {
					Jobs.Add(new BoosterJob(Bot, BoosterJobType.Limited, jobState));
				}
			}
		}

		internal string GetStatus(bool shortStatus = false) {
			// Queue empty
			Booster? nextBooster = Jobs.NextBooster();
			DateTime? limitedLastBoosterCraftTime = Jobs.Limited().LastBoosterCraftTime();
			if (nextBooster == null || (shortStatus && limitedLastBoosterCraftTime == null)) {
				if (BoosterQueue.IsUpdatingBoosterInfos()) {
					return Commands.FormatBotResponse(Bot, Strings.BoosterInfoUpdating);
				}

				return Commands.FormatBotResponse(Bot, Strings.QueueEmpty);
			}

			// Short status
			int limitedNumBoosters = Jobs.Limited().NumBoosters();
			int limitedNumUncraftedBoosters = Jobs.Limited().NumUncrafted();
			int limitedGemsNeeded = Jobs.Limited().GemsNeeded();
			if (shortStatus) {
				if (limitedLastBoosterCraftTime!.Value.Date == DateTime.Today) {
					return Commands.FormatBotResponse(Bot, String.Format(Strings.QueueStatusShort, limitedNumUncraftedBoosters, String.Format("{0:N0}", limitedGemsNeeded), String.Format("{0:t}", limitedLastBoosterCraftTime)));
				} else {
					return Commands.FormatBotResponse(Bot, String.Format(Strings.QueueStatusShortWithDate, limitedNumUncraftedBoosters, String.Format("{0:N0}", limitedGemsNeeded), String.Format("{0:d}", limitedLastBoosterCraftTime), String.Format("{0:t}", limitedLastBoosterCraftTime)));
				}
			}

			// Long status
			List<string> responses = new List<string>();

			// Refreshing booster page
			if (BoosterQueue.IsUpdatingBoosterInfos()) {
				responses.Add(Strings.BoosterInfoUpdating);
			}

			// Not enough gems
			int gemsNeeded = Jobs.GemsNeeded();
			if (gemsNeeded > BoosterQueue.AvailableGems) {
				responses.Add(String.Format("{0} :steamsad:", Strings.QueueStatusNotEnoughGems));

				if (nextBooster.Info.Price > BoosterQueue.AvailableGems) {
					responses.Add(String.Format(Strings.QueueStatusGemsNeeded, String.Format("{0:N0}", nextBooster.Info.Price - BoosterQueue.AvailableGems)));
				}

				if (Jobs.NumUncrafted() > 1) {
					responses.Add(String.Format(Strings.QueueStatusTotalGemsNeeded, String.Format("{0:N0}", gemsNeeded - BoosterQueue.AvailableGems)));
				}
			}

			// One-time booster status
			if (limitedNumBoosters > 0 && limitedLastBoosterCraftTime != null) {
				if (limitedLastBoosterCraftTime.Value.Date == DateTime.Today) {
					responses.Add(String.Format(Strings.QueueStatusLimitedBoosters, Jobs.Limited().NumCrafted(), limitedNumBoosters, String.Format("{0:t}", limitedLastBoosterCraftTime), String.Format("{0:N0}", limitedGemsNeeded)));
				} else {
					responses.Add(String.Format(Strings.QueueStatusLimitedBoostersWithDate, Jobs.Limited().NumCrafted(), limitedNumBoosters, String.Format("{0:d}", limitedLastBoosterCraftTime), String.Format("{0:t}", limitedLastBoosterCraftTime), String.Format("{0:N0}", limitedGemsNeeded)));
				}
				IEnumerable<string> gameIDStringsWithMultiples = Jobs.Limited().UncraftedGameIDs().GroupBy(x => x).Select(group => group.Count() == 1 ? group.Key.ToString() : String.Format("{0} (x{1})", group.Key, group.Count()));
				responses.Add(String.Format(Strings.QueueStatusLimitedBoosterList, String.Join(", ", gameIDStringsWithMultiples)));
			}

			// Permanent booster status
			if (Jobs.Permanent().NumBoosters() > 0) {
				responses.Add(String.Format(Strings.QueueStatusPermanentBoosters, String.Format("{0:N0}", Jobs.Permanent().GemsNeeded()), String.Join(", ", Jobs.Permanent().GameIDs())));
			}

			// Next booster to be crafted
			if (DateTime.Now > nextBooster.GetAvailableAtTime()) {
				responses.Add(String.Format(Strings.QueueStatusNextBoosterCraftingNow, nextBooster.Info.Name, nextBooster.GameID));
			} else {
				responses.Add(String.Format(Strings.QueueStatusNextBoosterCraftingLater, String.Format("{0:t}", nextBooster.GetAvailableAtTime()), nextBooster.Info.Name, nextBooster.GameID));
			}

			responses.Add("");

			return Commands.FormatBotResponse(Bot, String.Join(Environment.NewLine, responses));
		}
		
		internal uint GetGemsNeeded() {
			int gemsNeeded = Jobs.GemsNeeded();
			if (BoosterQueue.AvailableGems > gemsNeeded) {
				return 0;
			}

			return (uint) (gemsNeeded - BoosterQueue.AvailableGems);
		}

		internal void OnGemsRecieved() {
			if (GetGemsNeeded() == 0) {
				return;
			}

			// Refresh gems count
			BoosterQueue.OnBoosterInfosUpdated += BoosterQueue.ForceUpdateBoosterInfos;
			BoosterQueue.Start();
		}

		internal void OnBotLoggedOn() {
			BoosterQueue.Start();
		}

		internal static void GetBoosterInfos(HashSet<Bot> bots, Action<Dictionary<Bot, Dictionary<uint, Steam.BoosterInfo>>> callback) {
			ConcurrentDictionary<Bot, Dictionary<uint, Steam.BoosterInfo>> boosterInfos = new();

			foreach (Bot bot in bots) {
				BoosterQueue boosterQueue = BoosterHandlers[bot.BotName].BoosterQueue;

				void OnBoosterInfosUpdated(Dictionary<uint, Steam.BoosterInfo> boosterInfo) {
					boosterQueue.OnBoosterInfosUpdated -= OnBoosterInfosUpdated;
					boosterInfos.TryAdd(bot, boosterInfo);

					if (boosterInfos.Count == bots.Count) {
						callback(boosterInfos.ToDictionary());
					}
				}

				boosterQueue.OnBoosterInfosUpdated += OnBoosterInfosUpdated;
				boosterQueue.Start();
			}
		}
	}
}
