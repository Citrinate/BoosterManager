using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;

namespace BoosterManager {
	internal sealed class BoosterPageResponse {
		private readonly Bot Bot;
		internal readonly IEnumerable<Steam.BoosterInfo> BoosterInfos;
		internal readonly uint GooAmount;
		internal readonly uint TradableGooAmount;
		internal readonly uint UntradableGooAmount;

		internal BoosterPageResponse(Bot bot, IDocument? boosterPage) {
			Bot = bot;

			if (boosterPage == null) {
				Bot.ArchiLogger.LogNullError(boosterPage);

				throw new Exception();
			}

			MatchCollection gooAmounts = Regex.Matches(boosterPage.Source.Text, "(?<=parseFloat\\( \")[0-9]+");
			Match info = Regex.Match(boosterPage.Source.Text, "\\[\\{\"[\\s\\S]*\"}]");
			if (!info.Success || (gooAmounts.Count != 3)) {
				Bot.ArchiLogger.LogGenericError(string.Format(Strings.ErrorParsingObject, boosterPage));

				throw new Exception();
			}

			GooAmount = uint.Parse(gooAmounts[0].Value);
			TradableGooAmount = uint.Parse(gooAmounts[1].Value);
			UntradableGooAmount = uint.Parse(gooAmounts[2].Value);

			IEnumerable<Steam.BoosterInfo>? enumerableBoosters;
			try {
				enumerableBoosters = JsonSerializer.Deserialize<IEnumerable<Steam.BoosterInfo>>(info.Value, new JsonSerializerOptions { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });
			} catch (JsonException ex) {
				Bot.ArchiLogger.LogGenericError(ex.Message);

				throw new Exception();
			}
			if (enumerableBoosters == null) {
				Bot.ArchiLogger.LogNullError(enumerableBoosters);

				throw new Exception();
			}

			BoosterInfos = enumerableBoosters;
		}
	}
}
