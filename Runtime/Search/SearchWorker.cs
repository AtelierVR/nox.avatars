using Nox.Avatars.Runtime.Network;
using Cysharp.Threading.Tasks;
using Nox.Search;

namespace Nox.Avatars.Runtime.Search {
	public class SearchWorker : IWorker {
		public string Title;
		public string ServerAddress;

		public string GetTitleKey()
			=> "avatar.search.worker.title";

		public string[] GetTitleArguments()
			=> new[] { Title };

		public float GetRatio()
			=> 4f / 3f;

		public async UniTask<IResult> Fetch(IFetchOptions options) {
			if (string.IsNullOrEmpty(ServerAddress))
				return new SearchResult { Error = "Invalid server address." };
			var data = await Main.Instance.Network.Search(
				new SearchRequest {
					query  = options.GetQuery(),
					offset = options.GetPage() * options.GetLimit(),
					limit  = options.GetLimit(),
				}, ServerAddress
			);
			if (data == null) return new SearchResult { Error = "Error fetching users." };
			return new SearchResult {
				Response      = data,
				ServerAddress = ServerAddress,
				Error         = null
			};
		}
	}
}