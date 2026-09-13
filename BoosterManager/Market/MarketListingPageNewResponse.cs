using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using ArchiSteamFarm.Core;

namespace BoosterManager {
	internal static class MarketListingPageNewResponse {
		private static readonly Regex ContextDataRegex = new Regex("(?<=renderContext=JSON\\.parse\\(\").*?(?=\"\\);)", RegexOptions.CultureInvariant);

		internal static JsonObject Parse(IDocument? marketListingPage) {
		// internal static JsonObject Parse(string? marketListingPage) {
			if (marketListingPage == null) {
				ASF.ArchiLogger.LogNullError(marketListingPage);

				throw new Exception();
			}

			Match contextData = ContextDataRegex.Match(marketListingPage.Source.Text);
			// Match contextData = ContextDataRegex.Match(marketListingPage);
			if (!contextData.Success) {
				ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(marketListingPage)));

				throw new Exception();
			}

			// Convert escaped string into valid JSON
			string? cleanJson  = JsonNode.Parse(@"""" + contextData.Value + @"""")?.GetValue<string>();
			if (string.IsNullOrEmpty(cleanJson)) {
				ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorObjectIsNull, nameof(cleanJson)));

				throw new Exception();
			}

			JsonNode? context = JsonNode.Parse(cleanJson);
			if (context == null || context["queryData"] == null) {
				ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorObjectIsNull, nameof(context)));

				throw new Exception();
			}

			JsonNode? queryData = JsonNode.Parse(context["queryData"]!.ToString());
			if (queryData == null || queryData["queries"] == null) {
				ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorObjectIsNull, nameof(queryData)));

				throw new Exception();
			}

			JsonObject result = new();

			foreach (JsonNode queryItem in (IEnumerable<JsonNode>) queryData["queries"]!) {
				if (queryItem["queryKey"] == null) {
					ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorObjectIsNull, "queryItem['queryKey']"));

					throw new Exception();
				}

				IEnumerable<JsonNode> keys = (IEnumerable<JsonNode>) queryItem["queryKey"]!;
				JsonNode? data = queryItem["state"]?["data"];
				JsonObject cursor = result;

				for (int i = 0; i < keys.Count(); i++) {
					string key = keys.ElementAt(i).ToString();
					if (i == keys.Count() - 1) {
						cursor[key] = data == null ? null : data.DeepClone();
					} else {
						cursor[key] ??= new JsonObject();
						cursor = cursor[key]!.AsObject();
					}
				}
			}

			// Listing page doesn't return a 429 error, instead it's just missing data
			if (result["market"] == null) {
				ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorObjectIsNull, "result['market']"));

				throw new Exception();
			}

			return result;
		}
	}
}
