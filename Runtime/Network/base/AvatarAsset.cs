using System;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	// ReSharper disable InconsistentNaming
	[Serializable]
	public class AvatarAsset : IAvatarAsset, INoxObject {
		public uint     id;
		public ushort   version;
		public string   engine;
		public string   platform;
		public bool     is_empty;
		public string   url;
		public string[] features;
		public string   hash;
		public long     size;

		public uint GetId()
			=> id;

		public ushort GetVersion()
			=> version;

		public string GetEngine()
			=> engine;

		public string GetPlatform()
			=> platform;

		public bool IsEmpty()
			=> is_empty;

		public string GetUrl()
			=> url;

		public string[] GetFeatures()
			=> features ??= Array.Empty<string>();

		public string GetHash()
			=> hash;

		public long GetSize()
			=> size;
	}
}