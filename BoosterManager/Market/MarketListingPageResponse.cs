using System;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using ArchiSteamFarm.Core;

namespace BoosterManager {
	internal sealed class MarketListingPageResponse {
		internal readonly uint NameID;

		private readonly static Regex NameIDRegex = new Regex("(?<=Market_LoadOrderSpread\\( )[0-9]+", RegexOptions.CultureInvariant); // Matches number in: Market_LoadOrderSpread( 26463978 )

		internal MarketListingPageResponse(IDocument? marketListingPage) {
			if (marketListingPage == null) {
				ASF.ArchiLogger.LogNullError(marketListingPage);

				throw new Exception();
			}

			Match nameID = NameIDRegex.Match(marketListingPage.Source.Text);

			if (!nameID.Success) {
				ASF.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, nameof(marketListingPage)));

				throw new Exception();
			}

			NameID = uint.Parse(nameID.Value);
		}
	}
}
