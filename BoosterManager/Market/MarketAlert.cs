using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using BoosterManager.Localization;

namespace BoosterManager {
	internal sealed class MarketAlert {
		[JsonInclude]
		[JsonRequired]
		internal uint AppID { get; private set; }

		[JsonInclude]
		[JsonRequired]
		internal string HashName { get; private set; } = "";

		[JsonInclude]
		[JsonRequired]
		internal uint NameID { get; private set; }

		[JsonInclude]
		[JsonRequired]
		internal MarketAlertType Type { get; private set; }

		[JsonInclude]
		[JsonRequired]
		internal MarketAlertMode Mode { get; private set; }

		[JsonInclude]
		[JsonRequired]
		internal uint Amount { get; private set; }

		[JsonInclude]
		[JsonRequired]
		internal StatusReporter StatusReporter { get; private set; } = new(0, 0);

		[JsonConstructor]
		private MarketAlert() { }

		internal MarketAlert(uint appID, string hashName, uint nameID, MarketAlertType type, MarketAlertMode mode, uint amount, StatusReporter statusReporter) {
			AppID = appID;
			HashName = hashName;
			NameID = nameID;
			Type = type;
			Mode = mode;
			Amount = amount;
			StatusReporter = statusReporter;
		}

		internal void CheckAlert(Bot bot, uint? buyNowPrice, uint? sellNowPrice) {
			if (Type == MarketAlertType.Buy && buyNowPrice != null) {
				if ((Mode == MarketAlertMode.AboveOrAt && buyNowPrice >= Amount)
					|| (Mode == MarketAlertMode.BelowOrAt && buyNowPrice <= Amount)
					|| (Mode == MarketAlertMode.Above && buyNowPrice > Amount)
					|| (Mode == MarketAlertMode.Below && buyNowPrice < Amount)
				) {
					Resolve(bot, buyNowPrice.Value);					
				}
			} else if (Type == MarketAlertType.Sell && sellNowPrice != null) {
				if ((Mode == MarketAlertMode.AboveOrAt && sellNowPrice >= Amount)
					|| (Mode == MarketAlertMode.BelowOrAt && sellNowPrice <= Amount)
					|| (Mode == MarketAlertMode.Above && sellNowPrice > Amount)
					|| (Mode == MarketAlertMode.Below && sellNowPrice < Amount)
				) {
					Resolve(bot, sellNowPrice.Value);
				}
			}
		}

		internal void Resolve(Bot bot, uint currentPrice) {
			StatusReporter.Report(bot, String.Format(Strings.MarketAlertReached, 
				Type == MarketAlertType.Buy ? Strings.MarketAlertTypeBuy : Strings.MarketAlertTypeSell, 
				Mode == MarketAlertMode.AboveOrAt ? Strings.MarketAlertModeAbove : Strings.MarketAlertModeBelow, 
				String.Format(CultureInfo.CurrentCulture, "{0:#,#0.00}", Amount / 100.0), 
				bot.WalletCurrency, 
				AppID, 
				HashName,
				String.Format("!a {0} {1} {2} {3} {4} {5}", 
					bot.BotName,
					AppID,
					Uri.EscapeDataString(HashName),
					Type.ToString(),
					Mode.ToString(),
					String.Format(CultureInfo.CurrentCulture, "{0:#,#0.00}", Amount / 100.0)
				)
			));

			if (Type == MarketAlertType.Buy) {
				StatusReporter.Report(bot, String.Format(Strings.MarketAlertBuyPriceCurrentValue, String.Format(CultureInfo.CurrentCulture, "{0:#,#0.00}", currentPrice / 100.0), bot.WalletCurrency));
			} else {
				StatusReporter.Report(bot, String.Format(Strings.MarketAlertSellPriceCurrentValue, String.Format(CultureInfo.CurrentCulture, "{0:#,#0.00}", currentPrice / 100.0), bot.WalletCurrency));
			}
			
			StatusReporter.Report(bot, new Uri(ArchiWebHandler.SteamCommunityURL, String.Format("/market/listings/{0}/{1}", AppID, Uri.EscapeDataString(HashName))).AbsoluteUri);

			BoosterHandler.BoosterHandlers[bot.BotName].BotCache.RemoveMarketAlert(this);
		}
	}

	internal enum MarketAlertType {
		Buy,
		Sell
	}

	internal enum MarketAlertMode {
		AboveOrAt,
		BelowOrAt,
		Above,
		Below
	}

	internal class MarketAlertComparer : IEqualityComparer<MarketAlert>, IComparer<MarketAlert> {
		private static readonly Dictionary<MarketAlertMode, int> ModeOrder = new() {
			{ MarketAlertMode.AboveOrAt, 0 },
			{ MarketAlertMode.BelowOrAt, 1 },
			{ MarketAlertMode.Above, 0 },
			{ MarketAlertMode.Below, 1 },
		};

		public bool Equals(MarketAlert? x, MarketAlert? y) {
			if (x == null && y == null) {
				return true;
			} else if (x == null || y == null) {
				return false;
			}

			return x.HashName == y.HashName
				&& x.Type == y.Type
				&& x.Mode == y.Mode
				&& x.Amount == y.Amount;
		}

		public int Compare(MarketAlert? x, MarketAlert? y) {
			if (x == null && y == null) {
				return 0;
			} else if (x == null) {
				return -1;
			} else if (y == null) {
				return 1;
			}

			if (x.AppID > y.AppID) {
				return 1;
			} else if (x.AppID < y.AppID) {
				return -1;
			}

			int hashCompare = StringComparer.Ordinal.Compare(x.HashName, y.HashName);
			if (hashCompare != 0) {
				return hashCompare;
			}

			if ((int) x.Type > (int) y.Type) {
				return 1;
			} else if ((int) x.Type < (int) y.Type) {
				return -1;
			}

			if (ModeOrder[x.Mode] > ModeOrder[y.Mode]) {
				return 1;
			} else if (ModeOrder[x.Mode] < ModeOrder[y.Mode]) {
				return -1;
			}

			if (x.Amount > y.Amount) {
				return -1;
			} else if (x.Amount < y.Amount) {
				return 1;
			}

			if ((int) x.Mode > (int) y.Mode) {
				return 1;
			} else if ((int) x.Mode < (int) y.Mode) {
				return -1;
			}

			return 0;
		}

		public int GetHashCode(MarketAlert obj) {
			return HashCode.Combine(obj.AppID, obj.HashName, obj.Type, obj.Mode, obj.Amount);
		}
	}
}
