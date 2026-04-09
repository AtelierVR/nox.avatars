using Cysharp.Threading.Tasks;

namespace Nox.Avatars {
	public interface IAssetSearchResponse {
		
		public uint Total { get; }

		public uint Limit { get; }

		public uint Offset { get; }

		public IAvatarAsset[] Items { get; }

		public bool HasPrevious();

		public bool HasNext();

		public UniTask<IAssetSearchResponse> Previous();

		public UniTask<IAssetSearchResponse> Next();
	}
}