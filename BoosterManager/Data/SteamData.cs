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

		[JsonProperty(PropertyName = "data")]
		public T Data;

		internal SteamData(Bot bot, T steamData, Uri source, uint? page) {
			SteamID = bot.SteamID;
			Data = steamData;
			Source = source.ToString();
			Page = page;
		}
	}
}
