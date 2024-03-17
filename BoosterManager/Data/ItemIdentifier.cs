using System;
using System.Net;
using ArchiSteamFarm.Steam.Data;

namespace BoosterManager {
	internal sealed class ItemIdentifier {
		internal uint? AppID = null;
		internal ulong? ContextID = null;
		internal ulong? ClassID = null;
		internal string? TextID = null;
		internal EAssetType? Type = null;
		internal bool? Marketable = null;

		internal const string Delimiter = "&&";
		internal const string Separator = "::";

		internal static readonly ItemIdentifier GemIdentifier = new ItemIdentifier() { AppID = Asset.SteamAppID, ContextID = Asset.SteamCommunityContextID, ClassID = GemHandler.GemsClassID };
		internal static readonly ItemIdentifier SackIdentifier = new ItemIdentifier() { AppID = Asset.SteamAppID, ContextID = Asset.SteamCommunityContextID, ClassID = GemHandler.SackOfGemsClassID };
		internal static readonly ItemIdentifier GemAndSackIdentifier = new ItemIdentifier() { Type = EAssetType.SteamGems };
		internal static readonly ItemIdentifier CardIdentifier = new ItemIdentifier() { Type = EAssetType.TradingCard };
		internal static readonly ItemIdentifier FoilIdentifier = new ItemIdentifier() { Type = EAssetType.FoilTradingCard };
		internal static readonly ItemIdentifier BoosterIdentifier = new ItemIdentifier() { Type = EAssetType.BoosterPack };
		internal static readonly ItemIdentifier KeyIdentifier = new ItemIdentifier() { TextID = KeyHandler.MarketHash };

		internal ItemIdentifier() {}
		
		internal ItemIdentifier(string identityString, bool? marketable = null) {
			Marketable = marketable;

			string[] ids = identityString.Split(Separator);
			uint appID;
			ulong contextID;
			if (ids.Length == 2 && uint.TryParse(ids[0], out appID) && ulong.TryParse(ids[1], out contextID)) {
				// Format: AppID::ContextID
				AppID = appID;
				ContextID = contextID;
			} else if (ids.Length == 3 && uint.TryParse(ids[0], out appID) && ulong.TryParse(ids[1], out contextID) && ulong.TryParse(ids[2], out ulong classID)) {
				// Format: AppID::ContextID::ClassID
				AppID = appID;
				ContextID = contextID;
				ClassID = classID;
			} else if (ids.Length == 2 && ids[0] == "Type" && Enum.TryParse<EAssetType>(ids[1], out EAssetType type)) {
				// Format: Type::TypeEnum
				// Note: This format is useful internally, but not publicly documented
				// This is because it only works with inventory items and not items listed on the marketplace
				Type = type;
			} else {
				// Assumed format: ItemName, ItemType, or HashName
				TextID = identityString;
			}
		}

		public override string ToString() {
			if (AppID != null && ContextID != null) {
				if (ClassID != null) {
					return String.Format("{1}{0}{2}{0}{3}", Separator, AppID, ContextID, ClassID);
				} else {
					return String.Format("{1}{0}{2}", Separator, AppID, ContextID);
				}
			}

			if (Type != null) {
				return String.Format("Type{0}{1}", Separator, Type.ToString());
			}

			if (TextID != null) {
				return TextID;
			}

			return "Invalid";
		}

		internal bool IsItemMatch(Asset item) {
			if (AppID == null && ContextID == null && ClassID == null && Type == null && TextID == null) {
				return false;
			}

			if (AppID != null && item.AppID != AppID) {
				return false;
			}

			if (ContextID != null && item.ContextID != ContextID) {
				return false;
			}

			if (ClassID != null && item.ClassID != ClassID) {
				return false;
			}

			if (Type != null && item.Type != Type) {
				return false;
			}

			if (TextID != null) {
				string? name = item.Description?.Name;
				string? marketName = item.Description?.MarketName;
				string? marketHashName = item.Description?.MarketHashName;
				string? type = item.Description?.TypeText;
				
				if ((name == null || !name.Contains(TextID))
					&& (marketName == null || !marketName.Contains(TextID))
					&& (marketHashName == null || (!marketHashName.Contains(TextID) && !marketHashName.Contains(WebUtility.UrlDecode(TextID))))
					&& (type == null || !type.Contains(TextID))
				) {
					return false;
				}
			}

			if (Marketable != null && item.Marketable != Marketable) {
				return false;
			}

			return true;
		}

		internal bool IsItemListingMatch(ItemListing item) {
			if (AppID == null && ContextID == null && ClassID == null && TextID == null) {
				return false;
			}

			if (AppID != null && item.AppID != AppID) {
				return false;
			}

			if (ContextID != null && item.ContextID != ContextID) {
				return false;
			}

			if (ClassID != null && item.ClassID != ClassID) {
				return false;
			}

			if (TextID != null) {
				if (!item.Name.Equals(TextID)
					&& !item.MarketName.Equals(TextID)
					&& !item.MarketHashName.Equals(TextID)
					&& !item.MarketHashName.Equals(WebUtility.UrlDecode(TextID))
					&& !item.Type.Equals(TextID)
				) {
					return false;
				}
			}
			
			return true;
		}
	}
}
