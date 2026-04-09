using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	[Serializable]
	public class AssetSearchResponse : IAssetSearchResponse, INoxObject {
		[JsonIgnore] internal Identifier Identifier;
		[JsonIgnore] internal AssetSearchRequest Request;
		[JsonIgnore] internal string Server;

		[JsonProperty("total")]
		public uint Total { get; private set; }

		[JsonProperty("limit")]
		public uint Limit { get; private set; }

		[JsonProperty("offset")]
		public uint Offset { get; private set; }

		[JsonProperty("items")]
		public AvatarAsset[] Items { get; private set; }

		IAvatarAsset[] IAssetSearchResponse.Items
			=> Items.ToArray<IAvatarAsset>();


		public bool HasNext()
			=> Offset + Limit < Total;

		public bool HasPrevious()
			=> Offset > 0;

		async UniTask<IAssetSearchResponse> IAssetSearchResponse.Previous()
			=> await Previous();

		private UniTask<AssetSearchResponse> Previous()
			=> HasNext()
				? Main.Instance.Network.SearchAssets(
					Identifier,
					new AssetSearchRequest {
						Offset    = Offset >= Limit ? Offset - Limit : 0,
						Limit     = Limit,
						ShowEmpty = Request.ShowEmpty,
						Versions  = Request.Versions,
						Engines   = Request.Engines,
						Platforms = Request.Platforms
					},
					Server
				)
				: default;

		async UniTask<IAssetSearchResponse> IAssetSearchResponse.Next()
			=> await Next();

		private UniTask<AssetSearchResponse> Next()
			=> HasPrevious()
				? Main.Instance.Network.SearchAssets(
					Identifier,
					new AssetSearchRequest {
						Offset    = Offset + Limit,
						Limit     = Limit,
						ShowEmpty = Request.ShowEmpty,
						Versions  = Request.Versions,
						Engines   = Request.Engines,
						Platforms = Request.Platforms
					},
					Server
				)
				: default;

	}
}