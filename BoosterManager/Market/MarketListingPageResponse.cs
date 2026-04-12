using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using ArchiSteamFarm.Core;

namespace BoosterManager {
	internal sealed class MarketListingPageResponse {
		[JsonInclude]
		[JsonPropertyName("app")]
		internal readonly JsonElement App;

		[JsonInclude]
		[JsonPropertyName("asset")]
		internal readonly JsonElement Asset;

		[JsonInclude]
		[JsonPropertyName("name_id")]
		internal readonly uint NameID;

		private readonly static Regex AppContextDataRegex = new Regex("(?<=var\\s*g_rgAppContextData\\s*=\\s*)\\{.*?\\}(?=;\n)", RegexOptions.CultureInvariant); // Matches the object in: var g_rgAppContextData = { ... };
		private readonly static Regex AssetsRegex = new Regex("(?<=var\\s*g_rgAssets\\s*=\\s*)\\{.*?\\}(?=;\n)", RegexOptions.CultureInvariant); // Matches the object in: var g_rgAssets = { ... };
		private readonly static Regex NameIDRegex = new Regex("(?<=Market_LoadOrderSpread\\( )[0-9]+", RegexOptions.CultureInvariant); // Matches number in: Market_LoadOrderSpread( 26463978 )

		internal MarketListingPageResponse(IDocument? marketListingPage) {
			if (marketListingPage == null) {
				ASF.ArchiLogger.LogNullError(marketListingPage);

				throw new Exception();
			}

			Match appContextData = AppContextDataRegex.Match(marketListingPage.Source.Text);
			Match assets = AssetsRegex.Match(marketListingPage.Source.Text);
			Match nameID = NameIDRegex.Match(marketListingPage.Source.Text);

			if (!appContextData.Success || !assets.Success|| !nameID.Success) {
				ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(marketListingPage)));

				throw new Exception();
			}

			// Undo some of the object nesting so that the important data is on the top layer 
			// If the item is a non-commodity item there may be multiple assets, but I just want one. Doesn't matter which one
			// NOTE: For some market items this nesting is done with objects: {"753":{"6":{"38135120805":{}}}} (https://steamcommunity.com/market/listings/753/753-Sack of Gems)
			// For other items the nesting is done with arrays: {"730":[[{}]]} (https://steamcommunity.com/market/listings/730/AWP%20%7C%20Dragon%20Lore%20%28Factory%20New%29)
			App = JsonDocument.Parse(appContextData.Value).RootElement.GetFirstNested().Clone();
			Asset = JsonDocument.Parse(assets.Value).RootElement.GetFirstNested().GetFirstNested().GetFirstNested().Clone();

			NameID = uint.Parse(nameID.Value);
		}
	}

	internal static class MarketListingPageResponseJsonExtensions {
		internal static JsonElement GetFirstNested(this JsonElement rootElement) {
			if (rootElement.ValueKind == JsonValueKind.Object) {
				JsonElement.ObjectEnumerator enumerator = rootElement.EnumerateObject();
				if (!enumerator.Any()) {
					return rootElement;
				}

				return enumerator.First().Value;
			} else if (rootElement.ValueKind == JsonValueKind.Array) {
				if (rootElement.GetArrayLength() == 0) {
					return rootElement;
				}

				return rootElement[0];
			}

			return rootElement;
		}
	}
}
