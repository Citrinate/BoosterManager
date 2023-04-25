using System;
using System.Net;

namespace BoosterManager {
	internal sealed class ItemIdentifier {
		internal uint? AppID = null;
		internal ulong? ContextID = null;
		internal ulong? ClassID = null;
		internal string? TextID = null;
		internal string IdentityString;
		
		internal ItemIdentifier(string identityString, bool requireNumericIDs = false, bool requireClassID = false) {
			this.IdentityString = identityString;

			string[] ids = identityString.Split("::");
			uint appID;
			ulong contextID;
			ulong classID;
			if (ids.Length == 2
				&& uint.TryParse(ids[0], out appID)
				&& ulong.TryParse(ids[1], out contextID)
				&& !requireClassID
			) {
				// Format: AppID::ContextID
				this.AppID = appID;
				this.ContextID = contextID;
			} else if (ids.Length == 3
				&& uint.TryParse(ids[0], out appID)
				&& ulong.TryParse(ids[1], out contextID)
				&& ulong.TryParse(ids[2], out classID)
			) {
				// Format: AppID::ContextID::ClassID
				this.AppID = appID;
				this.ContextID = contextID;
				this.ClassID = classID;
			} else if (!requireNumericIDs) {
				// Assumed format: ItemName, ItemType, or HashName
				this.TextID = identityString;
			} else {
				throw new FormatException();
			}
		}

		internal bool isStringMatch(string text, bool urlDecode = false) {
			if (urlDecode) {
				return text.Equals(WebUtility.UrlDecode(TextID));
			}

			return text.Equals(TextID);
		}

		internal bool isNumericIDMatch(uint appID, ulong contextID, ulong classID) {
			return (
				(this.ClassID == null || classID == this.ClassID)
				&& contextID == this.ContextID
				&& appID == this.AppID
			);
		}
	}
}
