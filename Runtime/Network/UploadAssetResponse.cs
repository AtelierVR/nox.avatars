using System;
using Nox.Avatars;

namespace Nox.Avatars.Runtime.Network {
	[Serializable]
	public class UploadAssetResponse : IUploadAssetResponse {
		public bool   success;
		public string message;
		public string status;
		public int    progress;
		public int    queue_position;

		public bool Success => success;
		public string Message => message;
		public string Status => status;
		public int Progress => progress;
		public int QueuePosition => queue_position;
	}
}

