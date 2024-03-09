using System;
using System.Text.Json.Serialization;
using ArchiSteamFarm.Steam;

namespace BoosterManager {
	internal sealed class SteamData<T> {
		[JsonInclude]
		[JsonPropertyName("steamid")]
		public ulong SteamID { get; private init; }

		[JsonInclude]
		[JsonPropertyName("source")]
		public string Source { get; private init; }

		[JsonInclude]
		[JsonPropertyName("page")]
		public uint? Page { get; private init; }

		[JsonInclude]
		[JsonPropertyName("cursor")]
		public Steam.InventoryHistoryCursor? Cursor { get; private init; }

		[JsonInclude]
		[JsonPropertyName("data")]
		public T Data { get; private init; }

		internal SteamData(Bot bot, T steamData, Uri source, uint? page, Steam.InventoryHistoryCursor? cursor) {
			SteamID = bot.SteamID;
			Data = steamData;
			Source = source.ToString();
			Page = page;
			Cursor = cursor;
		}
	}
}
