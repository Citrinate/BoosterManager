using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using SteamKit2;

#pragma warning disable 649
namespace BoosterCreator {
	internal static class Steam {
		internal class EResultResponse {
			[JsonProperty(PropertyName = "success", Required = Required.Always)]
			public readonly EResult Result;

			[JsonConstructor]
			public EResultResponse() { }
		}

		internal sealed class BoosterInfo {
			[JsonProperty(PropertyName = "appid", Required = Required.Always)]
			internal readonly uint AppID;

			[JsonProperty(PropertyName = "name", Required = Required.Always)]
			internal readonly string Name;

			[JsonProperty(PropertyName = "series", Required = Required.Always)]
			internal readonly uint Series;

			[JsonProperty(PropertyName = "price", Required = Required.Always)]
			internal readonly uint Price;

			[JsonProperty(PropertyName = "unavailable", Required = Required.DisallowNull)]
			internal readonly bool Unavailable;

			[JsonProperty(PropertyName = "available_at_time", Required = Required.DisallowNull)]
			internal readonly DateTime AvailableAtTime;

			[JsonConstructor]
			private BoosterInfo() => Name = "";
		}

		internal sealed class BoostersResponse {
			[JsonProperty(PropertyName = "goo_amount", Required = Required.Always)]
			internal readonly uint GooAmount;

			[JsonProperty(PropertyName = "tradable_goo_amount", Required = Required.Always)]
			internal readonly uint TradableGooAmount;

			[JsonProperty(PropertyName = "untradable_goo_amount", Required = Required.Always)]
			internal readonly uint UntradableGooAmount;

			[JsonProperty(PropertyName = "purchase_result", Required = Required.DisallowNull)]
			internal readonly EResultResponse Result;

			[JsonProperty(PropertyName = "purchase_eresult", Required = Required.DisallowNull)]
			internal readonly EResult PurchaseEResult;

			[JsonConstructor]
			private BoostersResponse() => Result = new EResultResponse();
		}

		// https://stackoverflow.com/a/51319347
		internal sealed class BoosterInfoDateConverter : JsonConverter {
			private List<string> DateTimeFormats = new List<string>() {
				"MMM d @ h:mmtt",
				"MMM d, yyyy @ h:mmtt",
				"d MMM @ h:mmtt",
				"d MMM, yyyy @ h:mmtt"
			};
			
			public override bool CanConvert(Type objectType) {
				return objectType == typeof(DateTime);
			}
			
			public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
				if (reader.Value == null) {
					throw new JsonException("Unable to parse null as a date.");
				}
				string dateString = (string)reader.Value;
				DateTime date;
				foreach (string format in DateTimeFormats) {
					if (DateTime.TryParseExact(dateString, format, new CultureInfo("en-US"), DateTimeStyles.None, out date)) {
						return date;
					}
				}
				throw new JsonException("Unable to parse \"" + dateString + "\" as a date.");
			}
			
			public override bool CanWrite {
				get { return false; }
			}
			
			public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
				throw new NotImplementedException();
			}
		}
	}
}
#pragma warning restore 649
