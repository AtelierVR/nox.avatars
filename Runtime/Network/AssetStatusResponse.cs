using System;
using Nox.Avatars;

namespace Nox.Avatars.Runtime.Network {
	[Serializable]
	public class AssetStatusResponse : IAssetStatusResponse {
		public uint   asset_id;
		public string status;
		public int    progress;
		public int    queue_position;
		public string started_at;
		public string error;
		public string hash;
		public long   size;

		public uint AssetId => asset_id;
		public string Status => status;
		public int Progress => progress;
		public int QueuePosition => queue_position;
		public string StartedAt => started_at;
		public string Error => error;
		public string Hash => hash;
		public long Size => size;
	}
}

