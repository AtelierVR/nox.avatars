using System;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	[Serializable]
	public class AssetSearchResponse : IAssetSearchResponse, INoxObject {
		public AvatarAsset[] assets = Array.Empty<AvatarAsset>();
		public uint          total;
		public uint          limit;
		public uint          offset;

		public uint GetTotal()
			=> total;

		public uint GetLimit()
			=> limit;

		public uint GetOffset()
			=> offset;

		public AvatarAsset[] GetInternalAssets()
			=> assets ?? Array.Empty<AvatarAsset>();

		public IAvatarAsset[] GetAssets()
			=> GetInternalAssets().Cast<IAvatarAsset>().ToArray();
	}
}