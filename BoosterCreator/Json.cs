using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using SteamKit2;
#pragma warning disable 649
namespace BoosterCreator {
	internal static class Steam {
		internal class EResultResponse {
			[JsonProperty(PropertyName = "success", Required = Required.Always)]
			public readonly EResult Result;

			[JsonConstructor]
			protected EResultResponse() { }
		}

		[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
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
			internal readonly string AvailableAtTime;

			[JsonConstructor]
			private BoosterInfo() { }
		}

		[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
		internal sealed class BoostersResponse {
			[JsonProperty(PropertyName = "goo_amount", Required = Required.Always)]
			internal readonly uint GooAmount;

			[JsonProperty(PropertyName = "tradable_goo_amount", Required = Required.Always)]
			internal readonly uint TradableGooAmount;

			[JsonProperty(PropertyName = "untradable_goo_amount", Required = Required.Always)]
			internal readonly uint UntradableGooAmount;

			[JsonProperty(PropertyName = "purchase_result", Required = Required.DisallowNull)]
			internal readonly PurchaseResult Result;

			internal sealed class PurchaseResult : EResultResponse {
				[JsonConstructor]
				private PurchaseResult() { }
			}

			[JsonConstructor]
			private BoostersResponse() { }
		}
	}
}
#pragma warning restore 649
