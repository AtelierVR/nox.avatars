namespace Nox.Avatars {
	public interface ICreateAssetRequest {
		public ICreateAssetRequest SetId(uint i);

		public ICreateAssetRequest SetVersion(ushort v);

		public ICreateAssetRequest SetEngine(string e);

		public ICreateAssetRequest SetPlatform(string p);

		public ICreateAssetRequest SetUrl(string u);

		public ICreateAssetRequest SetHash(string h);

		public ICreateAssetRequest SetSize(long s);

		public uint GetId();

		public ushort GetVersion();

		public string GetEngine();

		public string GetPlatform();

		public string GetUrl();

		public string GetHash();

		public long GetSize();
	}
}