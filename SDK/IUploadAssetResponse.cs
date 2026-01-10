namespace Nox.Avatars {
	public interface IUploadAssetResponse {
		bool Success { get; }
		string Message { get; }
		string Status { get; }
		int Progress { get; }
		int QueuePosition { get; }
	}
}

