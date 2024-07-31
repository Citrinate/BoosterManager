using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Steam;

namespace BoosterManager {
	internal sealed class BoosterPageResponse {
		private readonly Bot Bot;
		internal readonly IEnumerable<Steam.BoosterInfo> BoosterInfos;
		internal readonly uint GooAmount;
		internal readonly uint TradableGooAmount;
		internal readonly uint UntradableGooAmount;
		private readonly static Regex GooAmounts = new Regex("(?<=parseFloat\\( \")[0-9]+", RegexOptions.CultureInvariant);
		private readonly static Regex Info = new Regex("\\[\\{\"[\\s\\S]*\"}]", RegexOptions.CultureInvariant);

		internal BoosterPageResponse(Bot bot, IDocument? boosterPage) {
			Bot = bot;

			if (boosterPage == null) {
				Bot.ArchiLogger.LogNullError(boosterPage);

				throw new Exception();
			}

			MatchCollection gooAmounts = GooAmounts.Matches(boosterPage.Source.Text);
			Match info = Info.Match(boosterPage.Source.Text);

			if (!info.Success || (gooAmounts.Count != 3)) {
				Bot.ArchiLogger.LogGenericError(string.Format(ArchiSteamFarm.Localization.Strings.ErrorParsingObject, boosterPage));

				throw new Exception();
			}

			GooAmount = uint.Parse(gooAmounts[0].Value);
			TradableGooAmount = uint.Parse(gooAmounts[1].Value);
			UntradableGooAmount = uint.Parse(gooAmounts[2].Value);

			IEnumerable<Steam.BoosterInfo>? enumerableBoosters;
			try {
				enumerableBoosters = info.Value.ToJsonObject<IEnumerable<Steam.BoosterInfo>>();
			} catch (JsonException) {
				throw;
			}

			if (enumerableBoosters == null) {
				Bot.ArchiLogger.LogNullError(enumerableBoosters);

				throw new Exception();
			}

			BoosterInfos = enumerableBoosters;
		}
	}
}
