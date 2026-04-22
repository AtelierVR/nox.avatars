using System;
using Newtonsoft.Json;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	// ReSharper disable InconsistentNaming
	[Serializable]
	public class AvatarAsset : IAvatarAsset, INoxObject {
		[JsonProperty("id")]
		public uint Id { get; private set; }

		[JsonProperty("version")]
		public ushort Version { get; private set; }

		[JsonProperty("engine")]
		public string Engine  { get; private set; }

		[JsonProperty("platform")]
		public string Platform { get; private set; }

		[JsonProperty("is_empty")]
		public bool IsEmpty { get; private set; }

		[JsonProperty("url")]
		public string Url { get; private set; }

		[JsonProperty("features")]
		public string[] Features  { get; private set; }
		
		[JsonProperty("hash")]
		public string Hash  { get; private set; }

		[JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
		public long Size { get; private set; }
	}
}