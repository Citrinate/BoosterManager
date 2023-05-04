using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ArchiSteamFarm.IPC.Controllers.Api;
using ArchiSteamFarm.IPC.Responses;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Annotations;

namespace BoosterManager {
	[Route("Api/BoosterManager", Name = nameof(BoosterManager))]
	public sealed class BoosterManagerController : ArchiController {
		/// <summary>
		///     Retrieves booster data for given bot.
		/// </summary>
		[HttpGet("{botName:required}/BoosterData")]
		[SwaggerOperation (Summary = "Retrieves booster data for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<SteamData<IEnumerable<Steam.BoosterInfo>>>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> BoosterData(string botName) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}
			
			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(Strings.BotNotFound, botName)));
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, Strings.BotNotConnected));
			}

			(BoosterPageResponse? boosterPage, Uri source) = await WebRequest.GetBoosterPage(bot).ConfigureAwait(false);
			if (boosterPage == null) {
				return BadRequest(new GenericResponse(false, "Failed to fetch Booster Data"));
			}

			return Ok(new GenericResponse<SteamData<IEnumerable<Steam.BoosterInfo>>>(true, new SteamData<IEnumerable<Steam.BoosterInfo>>(bot, boosterPage.BoosterInfos, source, null, null)));
		}

		/// <summary>
		///     Retrieves market listings data for given bot.
		/// </summary>
		[HttpGet("{botName:required}/MarketListings")]
		[SwaggerOperation (Summary = "Retrieves market listings data for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<SteamData<Steam.MarketListingsResponse>>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> MarketListings(string botName) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(Strings.BotNotFound, botName)));
			}

			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, Strings.BotNotConnected));
			}

			(Steam.MarketListingsResponse? marketListings, Uri source) = await WebRequest.GetMarketListings(bot).ConfigureAwait(false);
			if (marketListings == null || !marketListings.Success) {
				return BadRequest(new GenericResponse(false, "Failed to fetch Market Listings"));
			}

			return Ok(new GenericResponse<SteamData<Steam.MarketListingsResponse>>(true, new SteamData<Steam.MarketListingsResponse>(bot, marketListings, source, null, null)));
		}

		/// <summary>
		///     Retrieves market history data for given bot.
		/// </summary>
		[HttpGet("{botName:required}/MarketHistory")]
		[SwaggerOperation (Summary = "Retrieves market history data for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<SteamData<Steam.MarketHistoryResponse>>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> MarketHistory(string botName, uint page = 1) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(Strings.BotNotFound, botName)));
			}
			
			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, Strings.BotNotConnected));
			}

			uint count = 500;
			uint start = (page - 1) * count;
			(Steam.MarketHistoryResponse? marketHistory, Uri source) = await WebRequest.GetMarketHistory(bot, start, count).ConfigureAwait(false);
			if (marketHistory == null || !marketHistory.Success) {
				return BadRequest(new GenericResponse(false, "Failed to fetch Market Listings"));
			}

			return Ok(new GenericResponse<SteamData<Steam.MarketHistoryResponse>>(true, new SteamData<Steam.MarketHistoryResponse>(bot, marketHistory, source, page, null)));
		}

		/// <summary>
		///     Retrieves inventory history data for given bot.
		/// </summary>
		[HttpGet("{botName:required}/InventoryHistory")]
		[SwaggerOperation (Summary = "Retrieves inventory history data for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<SteamData<Steam.InventoryHistoryResponse>>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> InventoryHistory(string botName, uint? startTime = null, uint? timeFrac = null, string? s = null) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(Strings.BotNotFound, botName)));
			}
			
			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, Strings.BotNotConnected));
			}

			Steam.InventoryHistoryCursor? cursor = null;
			if (startTime != null && timeFrac != null && s != null) {
				cursor = new Steam.InventoryHistoryCursor(startTime.Value, timeFrac.Value, s);
			} else if (timeFrac != null || s != null) {
				return BadRequest(new GenericResponse(false, "When using 'timeFrac' or 's', all three parameters must be defined: 'startTime', 'timeFrac', and 's'"));
			}

			Steam.InventoryHistoryResponse? inventoryHistory = null; 
			Uri? source = null;
			try {
				(inventoryHistory, source) = await WebRequest.GetInventoryHistory(bot, DataHandler.InventoryHistoryAppFilter, cursor, startTime).ConfigureAwait(false);
			} catch (InventoryHistoryException) {
				return BadRequest(new GenericResponse(false, "Rate limit exceeded"));
			}
			if (inventoryHistory == null || !inventoryHistory.Success) {
				return BadRequest(new GenericResponse(false, "Failed to fetch Inventory History"));
			}

			return Ok(new GenericResponse<SteamData<Steam.InventoryHistoryResponse>>(true, new SteamData<Steam.InventoryHistoryResponse>(bot, inventoryHistory, source!, startTime, cursor)));
		}

		/// <summary>
		///     Retrieves badge info for given bot.
		/// </summary>
		[HttpGet("{botName:required}/GetBadgeInfo/{appID:required}")]
		[SwaggerOperation (Summary = "Retrieves badge info for given bot.")]
		[ProducesResponseType(typeof(GenericResponse<JToken>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> GetBadgeInfo(string botName, uint appID, uint border = 0) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(Strings.BotNotFound, botName)));
			}
			
			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, Strings.BotNotConnected));
			}

			JToken? badgeInfo = await WebRequest.GetBadgeInfo(bot, appID, border).ConfigureAwait(false);
			if (badgeInfo == null) {
				return BadRequest(new GenericResponse(false, "Failed to fetch badge info"));
			}

			return Ok(new GenericResponse<JToken>(true, badgeInfo));
		}

		/// <summary>
		///     Retrieves price history for market items.
		/// </summary>
		[HttpGet("{botName:required}/GetPriceHistory/{appID:required}/{hashName:required}")]
		[SwaggerOperation (Summary = "Retrieves price history for market items.")]
		[ProducesResponseType(typeof(GenericResponse<JToken>), (int) HttpStatusCode.OK)]
		[ProducesResponseType(typeof(GenericResponse), (int) HttpStatusCode.BadRequest)]
		public async Task<ActionResult<GenericResponse>> GetPriceHistory(string botName, uint appID, string hashName) {
			if (string.IsNullOrEmpty(botName)) {
				throw new ArgumentNullException(nameof(botName));
			}

			Bot? bot = Bot.GetBot(botName);
			if (bot == null) {
				return BadRequest(new GenericResponse(false, string.Format(Strings.BotNotFound, botName)));
			}
			
			if (!bot.IsConnectedAndLoggedOn) {
				return BadRequest(new GenericResponse(false, Strings.BotNotConnected));
			}

			JToken? priceHistory = await WebRequest.GetPriceHistory(bot, appID, hashName).ConfigureAwait(false);
			if (priceHistory == null) {
				return BadRequest(new GenericResponse(false, "Failed to fetch market listings"));
			}

			return Ok(new GenericResponse<JToken>(true, priceHistory));
		}
	}
}