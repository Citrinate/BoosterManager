using System;
using ArchiSteamFarm.Core;
using Newtonsoft.Json.Linq;

namespace BoosterManager {
	internal sealed class ItemListing {
		internal string Name;
		internal string MarketName;
		internal string MarketHashName;
		internal string Type;
		internal uint AppID;
		internal ulong ContextID;
		internal ulong ClassID;

		internal ItemListing(JObject listing) {
			string? name = listing["asset"]?["name"]?.ToString();
			if (name == null) {
				ASF.ArchiLogger.LogNullError(name);				
				throw new InvalidOperationException();
			}

			string? marketName = listing["asset"]?["market_name"]?.ToString();
			if (marketName == null) {
				ASF.ArchiLogger.LogNullError(marketName);
				throw new InvalidOperationException();
			}

			string? marketHashName = listing["asset"]?["market_hash_name"]?.ToString();
			if (marketHashName == null) {
				ASF.ArchiLogger.LogNullError(marketHashName);				
				throw new InvalidOperationException();
			}

			string? type = listing["asset"]?["type"]?.ToString();
			if (type == null) {
				ASF.ArchiLogger.LogNullError(type);
				throw new InvalidOperationException();
			}

			uint? appID = listing["asset"]?["appid"]?.ToObject<uint>();
			if (appID == null) {
				ASF.ArchiLogger.LogNullError(appID);
				throw new InvalidOperationException();
			}

			ulong? contextID = listing["asset"]?["contextid"]?.ToObject<ulong>();
			if (contextID == null) {
				ASF.ArchiLogger.LogNullError(contextID);
				throw new InvalidOperationException();
			}

			ulong? classID = listing["asset"]?["classid"]?.ToObject<ulong>();
			if (classID == null) {
				ASF.ArchiLogger.LogNullError(classID);
				throw new InvalidOperationException();
			}
			
			Name = name;
			MarketName = marketName;
			MarketHashName = marketHashName;
			Type = type;
			AppID = appID.Value;
			ContextID = contextID.Value;
			ClassID = classID.Value;
		}
	}
}
