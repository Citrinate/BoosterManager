using System.Text.Json.Serialization;

namespace BoosterManager.IPC {
	public sealed class CreateListingRequest {
		[JsonInclude]
		[JsonRequired]
		public uint AppID { get; private init; }

		[JsonInclude]
		[JsonRequired]
		public ulong ContextID { get; private init; }

		[JsonInclude]
		[JsonRequired]
		public ulong AssetID { get; private init; }

		[JsonInclude]
		[JsonRequired]
		public uint Price { get; private init; }

		[JsonInclude]
		public uint Amount { get; private init; } = 1;

		[JsonConstructor]
		private CreateListingRequest() { }
	}
}