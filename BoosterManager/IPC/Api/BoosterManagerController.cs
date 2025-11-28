using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using ArchiSteamFarm.IPC.Controllers.Api;
using ArchiSteamFarm.IPC.Responses;
using ArchiSteamFarm.Steam;
using BoosterManager.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BoosterManager {
	[Route("Api/BoosterManager")]
	public sealed class BoosterManagerController : ArchiController {
		[HttpGet("{botName:required}/BoosterData")]
		[EndpointSummary("Retrieves booster data for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<SteamData<IEnumerable<Steam.BoosterInfo>>>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> BoosterData(string botName) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}
			
			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botName)));
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, ArchiSteamFarm.Localization.Strings.BotNotConnected));
			}

			(BoosterPageResponse? boosterPage, Uri source) = await WebRequest.GetBoosterPage(bot).ConfigureAwait(false);
			if (boosterPage == null) {
				return BadRequest(new GenericResponse(false, Strings.BoosterDataFetchFailed));
			}

			return Ok(new GenericResponse<SteamData<IEnumerable<Steam.BoosterInfo>>>(true, new SteamData<IEnumerable<Steam.BoosterInfo>>(bot, boosterPage.BoosterInfos, source, null, null)));
		}

		[HttpGet("{botName:required}/MarketListings")]
		[EndpointSummary("Retrieves market listings data for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<SteamData<Steam.MarketListingsResponse>>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> MarketListings(string botName) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botName)));
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, ArchiSteamFarm.Localization.Strings.BotNotConnected));
			}

			(Steam.MarketListingsResponse? marketListings, Uri source) = await WebRequest.GetMarketListings(bot, 0, -1).ConfigureAwait(false);
			if (marketListings == null || !marketListings.Success) {
				return BadRequest(new GenericResponse(false, Strings.MarketListingsFetchFailed));
			}

			return Ok(new GenericResponse<SteamData<Steam.MarketListingsResponse>>(true, new SteamData<Steam.MarketListingsResponse>(bot, marketListings, source, null, null)));
		}

		[HttpGet("{botName:required}/MarketHistory")]
		[EndpointSummary("Retrieves market history data for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<SteamData<Steam.MarketHistoryResponse>>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> MarketHistory(string botName, uint page = 1) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botName)));
			}
			
			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, ArchiSteamFarm.Localization.Strings.BotNotConnected));
			}

			uint count = 500;
			uint start = (page - 1) * count;
			(Steam.MarketHistoryResponse? marketHistory, Uri source) = await WebRequest.GetMarketHistory(bot, start, count).ConfigureAwait(false);
			if (marketHistory == null || !marketHistory.Success) {
				return BadRequest(new GenericResponse(false, Strings.MarketListingsFetchFailed));
			}

			return Ok(new GenericResponse<SteamData<Steam.MarketHistoryResponse>>(true, new SteamData<Steam.MarketHistoryResponse>(bot, marketHistory, source, page, null)));
		}

		[HttpGet("{botName:required}/InventoryHistory")]
		[EndpointSummary("Retrieves inventory history data for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<SteamData<Steam.InventoryHistoryResponse>>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> InventoryHistory(string botName, uint? startTime = null, uint? timeFrac = null, string? s = null) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botName)));
			}
			
			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, ArchiSteamFarm.Localization.Strings.BotNotConnected));
			}

			Steam.InventoryHistoryCursor? cursor = null;
			if (startTime != null && timeFrac != null && s != null) {
				cursor = new Steam.InventoryHistoryCursor(startTime.Value, timeFrac.Value, s);
			} else if (timeFrac != null || s != null) {
				return BadRequest(new GenericResponse(false, Strings.InventoryHistoryInvalidCursor));
			}

			Steam.InventoryHistoryResponse? inventoryHistory = null; 
			Uri? source = null;
			try {
				(inventoryHistory, source) = await WebRequest.GetInventoryHistory(bot, DataHandler.InventoryHistoryAppFilter, cursor, startTime).ConfigureAwait(false);
			} catch (InventoryHistoryException) {
				return BadRequest(new GenericResponse(false, Strings.RateLimitExceeded));
			}
			if (inventoryHistory == null || !inventoryHistory.Success) {
				return BadRequest(new GenericResponse(false, Strings.InventoryHistoryFetchFailed));
			}

			return Ok(new GenericResponse<SteamData<Steam.InventoryHistoryResponse>>(true, new SteamData<Steam.InventoryHistoryResponse>(bot, inventoryHistory, source!, startTime, cursor)));
		}

		[HttpGet("{botName:required}/GetBadgeInfo/{appID:required}")]
		[EndpointSummary("Retrieves badge info for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<JsonDocument>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> GetBadgeInfo(string botName, uint appID, uint border = 0) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botName)));
			}
			
			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, ArchiSteamFarm.Localization.Strings.BotNotConnected));
			}

			JsonDocument? badgeInfo = await WebRequest.GetBadgeInfo(bot, appID, border).ConfigureAwait(false);
			if (badgeInfo == null) {
				return BadRequest(new GenericResponse(false, Strings.BadgeInfoFetchFailed));
			}

			return Ok(new GenericResponse<JsonDocument>(true, badgeInfo));
		}

		[HttpGet("{botNames:required}/GetPriceHistory/{appID:required}/{hashName:required}")]
		[EndpointSummary("Retrieves price history for market items.")]
		[ProducesResponseType(typeof(GenericResponse<JsonDocument>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> GetPriceHistory(string botNames, uint appID, string hashName) {
			if (string.IsNullOrEmpty(botNames)) {
				throw new ArgumentNullException(nameof(botNames));
			}

			HashSet<Bot>? bots = Bot.GetBots(botNames);
			if ((bots == null) || (bots.Count == 0)) {
				return BadRequest(new GenericResponse(false, string.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botNames)));
			}

			Bot? bot = bots.FirstOrDefault(static bot => bot.IsConnectedAndLoggedOn);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, ArchiSteamFarm.Localization.Strings.BotNotConnected));
			}
			
			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, ArchiSteamFarm.Localization.Strings.BotNotConnected));
			}

			JsonDocument? priceHistory = await WebRequest.GetPriceHistory(bot, appID, hashName).ConfigureAwait(false);
			if (priceHistory == null) {
				return BadRequest(new GenericResponse(false, Strings.PriceHistoryFetchFailed));
			}

			return Ok(new GenericResponse<JsonDocument>(true, priceHistory));
		}

		[HttpGet("{botName:required}/RemoveListing/{listingID:required}")]
		[EndpointSummary("Removes the given listing for the given bot.")]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> MarketListings(string botName, ulong listingID) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(ArchiSteamFarm.Localization.Strings.BotNotFound, botName)));
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, ArchiSteamFarm.Localization.Strings.BotNotConnected));
			}

			bool success = await WebRequest.RemoveListing(bot, listingID).ConfigureAwait(false);
			if (!success) {
				return BadRequest(new GenericResponse(false, string.Format(Strings.ListingsRemovedFailed, 0, 1, listingID)));
			}

			return Ok(new GenericResponse(true, string.Format(Strings.ListingsRemovedSuccess, 1)));
		}
	}
}