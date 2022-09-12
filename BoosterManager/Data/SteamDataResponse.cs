using Newtonsoft.Json;

namespace BoosterManager {
	internal sealed class SteamDataResponse {
		[JsonProperty(PropertyName = "success", Required = Required.Always)]
		internal readonly bool Success = false;

		[JsonProperty(PropertyName = "message", Required = Required.Default)]
		internal readonly string? Message = null;

		[JsonProperty(PropertyName = "show_message", Required = Required.DisallowNull)]
		internal readonly bool ShowMessage = true;
	}
}
