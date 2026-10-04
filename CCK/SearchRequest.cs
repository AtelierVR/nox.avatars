using Nox.Avatars;
using Nox.CCK.Utils;

namespace Nox.CCK.Avatars {
	public class SearchRequest : ISearchRequest, INoxObject {
		public string Server { get; set; } = null;

		public string Query { get; set; } = null;

		public uint Offset { get; set; } = 0;

		public uint Limit { get; set; } = 0;

		public static SearchRequest From(ISearchRequest request)
			=> new() {
				Server = request.Server,
				Query  = request.Query,
				Offset = request.Offset,
				Limit  = request.Limit
			};
	}
}