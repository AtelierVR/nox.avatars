namespace Nox.Avatars {
	public interface IUploadAssetResponse : IAssetStatusResponse {
		public bool Success { get; }
	}
}