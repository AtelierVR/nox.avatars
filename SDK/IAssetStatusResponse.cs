namespace Nox.Avatars {
	public interface IAssetStatusResponse {
		uint AssetId { get; }
		string Status { get; }
		int Progress { get; }
		int QueuePosition { get; }
		string StartedAt { get; }
		string Error { get; }
		string Hash { get; }
		long Size { get; }
	}
}

