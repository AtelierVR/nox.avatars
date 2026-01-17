using Newtonsoft.Json;

namespace Nox.Avatars.Runtime.Network {
	public class UploadAssetResponse : AssetStatusResponse, IUploadAssetResponse {
		[JsonProperty("success")]
		public bool Success { get; }
	}
}