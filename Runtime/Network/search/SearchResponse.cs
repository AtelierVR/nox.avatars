using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nox.CCK.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	[Serializable]
	public class SearchResponse : ISearchResponse, INoxObject {
		[JsonIgnore]
		internal ISearchRequest Request;

		[JsonProperty("total")]
		public uint Total { get; }

		[JsonProperty("offset")]
		public uint Offset { get; }

		[JsonProperty("limit")]
		public uint Limit { get; }

		[JsonProperty("items")]
		public Avatar[] Items { get; }

		IAvatar[] ISearchResponse.Items
			=> Items.ToArray<IAvatar>();


		public bool HasNext()
			=> Offset + Limit < Total;

		[NoxPublic(NoxAccess.Method)]
		public bool HasPrevious()
			=> Offset > Limit;

		public async UniTask<SearchResponse> Next()
			=> HasNext()
				? await Main.Instance.Network.Search(
					new SearchRequest {
						Query       = Request.Query,
						Identifiers = Request.Identifiers,
						Offset      = Offset + Limit,
						Limit       = Limit
					}
				)
				: null;

		async UniTask<ISearchResponse> ISearchResponse.Next()
			=> await Next();


		public async UniTask<SearchResponse> Previous()
			=> HasPrevious()
				? await Main.Instance.Network.Search(
					new SearchRequest {
						Query       = Request.Query,
						Identifiers = Request.Identifiers,
						Offset      = Offset - Limit,
						Limit       = Limit
					}
				)
				: null;

		async UniTask<ISearchResponse> ISearchResponse.Previous()
			=> await Previous();
	}
}