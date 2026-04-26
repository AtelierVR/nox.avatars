using System;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace Nox.CCK.Avatars {
	public class SearchRequest : ISearchRequest, INoxObject {
		public override string ToString() {
			var text = "";
			if (!string.IsNullOrEmpty(Query))
				text += (text.Length > 0 ? "&" : "") + $"query={Query}";
			if (Identifiers != null)
				foreach (var u in Identifiers)
					text += (text.Length > 0 ? "&" : "") + $"id={u}";
			if (Offset > 0)
				text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
			if (Limit > 0)
				text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
			return string.IsNullOrEmpty(text) ? "" : $"?{text}";
		}

		public string Server { get; set; } = null;

		public string Query { get; set; } = null;

		public Identifier[] Identifiers { get; set; } = Array.Empty<Identifier>();

		public uint Offset { get; set; } = 0;

		public uint Limit { get; set; } = 0;
	}
}