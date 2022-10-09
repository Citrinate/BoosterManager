using System;
using ArchiSteamFarm.Steam;
using Newtonsoft.Json;

namespace BoosterManager {
	internal sealed class SteamData<T> {
		[JsonProperty(PropertyName = "steamid")]
		public ulong SteamID;

		[JsonProperty(PropertyName = "source")]
		public string Source;

		[JsonProperty(PropertyName = "page")]
		public uint? Page;

		[JsonProperty(PropertyName = "cursor")]
		public Steam.InventoryHistoryCursor? Cursor;

		[JsonProperty(PropertyName = "data")]
		public T Data;

		internal SteamData(Bot bot, T steamData, Uri source, uint? page, Steam.InventoryHistoryCursor? cursor) {
			SteamID = bot.SteamID;
			Data = steamData;
			Source = source.ToString();
			Page = page;
			Cursor = cursor;
		}
	}
}
