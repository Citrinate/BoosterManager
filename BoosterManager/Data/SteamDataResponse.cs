using System.Text.Json.Serialization;

namespace BoosterManager {
	internal sealed class SteamDataResponse {
		[JsonInclude]
		[JsonPropertyName("success")]
		[JsonRequired]
		internal bool Success { get; private init; } = false;

		[JsonInclude]
		[JsonPropertyName("message")]
		internal string? Message { get; private init; } = null;

		[JsonInclude]
		[JsonPropertyName("show_message")]
		internal bool ShowMessage { get; private init; } = true;

		[JsonInclude]
		[JsonPropertyName("get_next_page")]
		internal bool GetNextPage { get; private init; } = false;

		[JsonInclude]
		[JsonPropertyName("next_page")]
		internal uint? NextPage { get; private init; } = null;

		[JsonInclude]
		[JsonPropertyName("next_cursor")]
		internal Steam.InventoryHistoryCursor? NextCursor { get; private init; } = null;

		[JsonConstructor]
		internal SteamDataResponse() { }
	}
}
