using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SteamKit2;

#pragma warning disable 649
namespace BoosterManager {
	internal static class Steam {
		internal class EResultResponse {
			[JsonInclude]
			[JsonPropertyName("success")]
			[JsonRequired]
			public EResult Result { get; private init; }

			[JsonConstructor]
			public EResultResponse() { }
		}

		internal sealed class BoosterInfo {
			[JsonInclude]
			[JsonPropertyName("appid")]
			[JsonRequired]
			internal uint AppID { get; private init; }

			[JsonInclude]
			[JsonPropertyName("name")]
			[JsonRequired]
			internal string Name { get; private init; } = "";

			[JsonInclude]
			[JsonPropertyName("series")]
			[JsonRequired]
			internal uint Series { get; private init; }

			[JsonInclude]
			[JsonPropertyName("price")]
			[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
			[JsonRequired]
			internal uint Price { get; private init; }

			[JsonInclude]
			[JsonPropertyName("unavailable")]
			internal bool Unavailable { get; private init; }

			[JsonInclude]
			[JsonPropertyName("available_at_time")]
			[JsonConverter(typeof(BoosterInfoDateConverter))]
			internal DateTime? AvailableAtTime { get; private init; }

			[JsonConstructor]
			public BoosterInfo() { }
		}

		internal sealed class BoostersResponse {
			[JsonInclude]
			[JsonPropertyName("goo_amount")]
			[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
			[JsonRequired]
			internal uint GooAmount { get; private init; }

			[JsonInclude]
			[JsonPropertyName("tradable_goo_amount")]
			[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
			[JsonRequired]
			internal uint TradableGooAmount { get; private init; }

			[JsonInclude]
			[JsonPropertyName("untradable_goo_amount")]
			[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
			[JsonRequired]
			internal uint UntradableGooAmount { get; private init; }

			[JsonInclude]
			[JsonPropertyName("purchase_result")]
			internal EResultResponse Result { get; private init; } = new();

			[JsonInclude]
			[JsonPropertyName("purchase_eresult")]
			internal EResult PurchaseEResult { get; private init; }

			[JsonConstructor]
			private BoostersResponse() { }
		}
		
		internal sealed class MarketListingsResponse {
			[JsonInclude]
			[JsonPropertyName("success")]
			[JsonRequired]
			internal bool Success { get; private init; }

			[JsonInclude]
			[JsonPropertyName("pagesize")]
			[JsonRequired]
			internal int PageSize { get; private init; }

			[JsonInclude]
			[JsonPropertyName("total_count")]
			[JsonRequired]
			internal uint TotalCount { get; private init; }

			[JsonInclude]
			[JsonPropertyName("assets")]
			[JsonRequired]
			internal JsonElement? Assets { get; private init; }

			[JsonInclude]
			[JsonPropertyName("start")]
			[JsonRequired]
			internal uint Start { get; private init; }

			[JsonInclude]
			[JsonPropertyName("num_active_listings")]
			[JsonRequired]
			internal uint NumActiveListings { get; private init; }

			[JsonInclude]
			[JsonPropertyName("listings")]
			[JsonRequired]
			internal List<JsonNode>? Listings { get; private init; }

			[JsonInclude]
			[JsonPropertyName("listings_on_hold")]
			[JsonRequired]
			internal List<JsonNode>? ListingsOnHold { get; private init; } = new();

			[JsonInclude]
			[JsonPropertyName("listings_to_confirm")]
			[JsonRequired]
			internal List<JsonNode> ListingsToConfirm { get; private init; } = new();

			[JsonInclude]
			[JsonPropertyName("buy_orders")]
			[JsonRequired]
			internal List<JsonNode> BuyOrders { get; private init; } = new();

			[JsonConstructor]
			private MarketListingsResponse() { }
		}

		internal sealed class MarketHistoryResponse {
			[JsonInclude]
			[JsonPropertyName("success")]
			[JsonRequired]
			internal bool Success { get; private init; }

			[JsonInclude]
			[JsonPropertyName("pagesize")]
			[JsonRequired]
			internal uint PageSize { get; private init; }

			[JsonInclude]
			[JsonPropertyName("total_count")]
			[JsonRequired]
			internal uint? TotalCount { get; private init; }

			[JsonInclude]
			[JsonPropertyName("start")]
			[JsonRequired]
			internal uint Start { get; private init; }

			[JsonInclude]
			[JsonPropertyName("assets")]
			[JsonRequired]
			internal JsonElement? Assets { get; private init; }

			[JsonInclude]
			[JsonPropertyName("events")]
			internal List<JsonNode>? Events { get; private init; }

			[JsonInclude]
			[JsonPropertyName("purchases")]
			internal JsonElement? Purchases { get; private init; }

			[JsonInclude]
			[JsonPropertyName("listings")]
			[JsonRequired]
			internal JsonElement? Listings { get; private init; }

			[JsonConstructor]
			private MarketHistoryResponse() { }
		}

		internal sealed class InventoryHistoryCursor {
			[JsonInclude]
			[JsonPropertyName("time")]
			[JsonRequired]
			internal uint Time { get; private init; }

			[JsonInclude]
			[JsonPropertyName("time_frac")]
			[JsonRequired]
			internal uint TimeFrac { get; private init; }

			[JsonInclude]
			[JsonPropertyName("s")]
			[JsonRequired]
			internal string S { get; private init; } = "";

			[JsonConstructor]
			internal InventoryHistoryCursor() { }

			internal InventoryHistoryCursor(uint time, uint timeFrac, string s) {
				Time = time;
				TimeFrac = timeFrac;
				S = s;
			}
		}

		internal sealed class InventoryHistoryResponse {
			[JsonInclude]
			[JsonPropertyName("success")]
			[JsonRequired]
			internal bool Success { get; private init; }

			[JsonInclude]
			[JsonPropertyName("error")]
			internal string? Error { get; private init; } = "";

			[JsonInclude]
			[JsonPropertyName("html")]
			internal string Html { get; private init; } = "";

			[JsonInclude]
			[JsonPropertyName("num")]
			internal uint Num { get; private init; } = 0;

			[JsonInclude]
			[JsonPropertyName("descriptions")]
			internal JsonElement? Descriptions { get; private init; }

			[JsonInclude]
			[JsonPropertyName("apps")]
			internal List<JsonNode> Apps { get; private init; } = new();

			[JsonInclude]
			[JsonPropertyName("cursor")]
			internal InventoryHistoryCursor? Cursor { get; private init; }

			[JsonConstructor]
			private InventoryHistoryResponse() { }
		}

		internal sealed class ExchangeGooResponse {
			[JsonInclude]
			[JsonPropertyName("success")]
			[JsonRequired]
			internal int Success { get; private init; }

			[JsonConstructor]
			private ExchangeGooResponse() { }
		}

		internal sealed class ItemOrdersHistogramResponse {
			[JsonInclude]
			[JsonPropertyName("success")]
			[JsonRequired]
			internal int Success { get; private init; }

			[JsonInclude]
			[JsonPropertyName("highest_buy_order")]
			internal string? HighestBuyOrder { get; private init; }

			[JsonInclude]
			[JsonPropertyName("lowest_sell_order")]
			internal string? LowestSellOrder { get; private init; }

			[JsonInclude]
			[JsonPropertyName("buy_order_graph")]
			internal List<List<JsonElement>> BuyOrderGraph { get; private init; } = new();

			[JsonInclude]
			[JsonPropertyName("sell_order_graph")]
			internal List<List<JsonElement>> SellOrderGraph { get; private init; } = new();

			[JsonExtensionData]
			[JsonInclude]
			internal Dictionary<string, JsonElement> AdditionalData { get; private init; } = new();

			[JsonConstructor]
			private ItemOrdersHistogramResponse() { }
		}

		internal enum TradabilityPreference {
			Tradable = 1,
			Default = 2,
			Untradable = 3
		}

		// https://stackoverflow.com/a/51319347
		// internal sealed class BoosterInfoDateConverter : JsonConverter {
		internal sealed class BoosterInfoDateConverter : JsonConverter<DateTime> {
			private List<string> DateTimeFormats = new List<string>() {
				"MMM d @ h:mmtt",
				"MMM d, yyyy @ h:mmtt",
				"d MMM @ h:mmtt",
				"d MMM, yyyy @ h:mmtt"
			};
			
			public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
				string? dateString = reader.GetString();
				if (dateString == null) {
					throw new JsonException("Unable to parse null as a date.");
				}

				DateTime date;
				foreach (string format in DateTimeFormats) {
					if (DateTime.TryParseExact(dateString, format, new CultureInfo("en-US"), DateTimeStyles.None, out date)) {
						return date;
					}
				}

				throw new JsonException("Unable to parse \"" + dateString + "\" as a date.");
			}

			public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
				writer.WriteStringValue(value.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
			}
		}
	}
}
#pragma warning restore 649
