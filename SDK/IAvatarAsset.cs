namespace Nox.Avatars {
	public interface IAvatarAsset {
		public uint GetId();

		public ushort GetVersion();

		public string GetEngine();

		public string GetPlatform();

		public bool IsEmpty();

		public string GetUrl();

		public string[] GetFeatures();

		public string GetHash();

		public long GetSize();
	}
}