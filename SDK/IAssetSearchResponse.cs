namespace Nox.Avatars {
	public interface IAssetSearchResponse {
		
		public uint GetTotal();

		public uint GetLimit();

		public uint GetOffset();

		public IAvatarAsset[] GetAssets();
	}
}