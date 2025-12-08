using System;
using System.Linq;
using Nox.Avatars.Runtime.Network;
using Nox.Search;

namespace Nox.Avatars.Runtime.Search {
	public class SearchResult : IResult {
		public string         Error;
		public SearchResponse Response;
		public string         ServerAddress;
		public int            MenuId;

		public bool IsError()
			=> !string.IsNullOrEmpty(Error);

		public string GetError()
			=> Error;

		public bool HasNext()
			=> !IsError() && Response.HasNext();

		public IResultData[] GetData()
			=> Response != null
				? Response.avatars
					.Select(x => new SearchData { Reference = x })
					.Cast<IResultData>()
					.ToArray()
				: Array.Empty<IResultData>();
	}
}