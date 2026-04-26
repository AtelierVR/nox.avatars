using Nox.CCK.Utils;

namespace Nox.Avatars {
	/// <summary>
	/// Represents a search request for avatars,
	/// containing parameters for filtering and pagination.
	/// </summary>
	public interface ISearchRequest {
		/// <summary>
		/// Specify which server to search for avatars.
		/// If null or empty, the search will be performed on the current server.
		/// </summary>
		public string Server { get; set; }
		/// <summary>
		/// Gets or sets the search query string used to filter avatars based on their name,
		/// description, or other relevant attributes.
		/// </summary>
		public string Query { get; set; }

		/// <summary>
		/// Gets or sets an array based on strict search with specific identifiers.
		/// </summary>
		public Identifier[] Identifiers { get; set; }

		/// <summary>
		/// Gets or sets the offset for pagination,
		/// indicating the number of items to skip before starting
		/// to collect the result set.
		/// </summary>
		public uint Offset { get; set; }

		/// <summary>
		/// Gets or sets the limit for pagination,
		/// indicating the maximum number of items to return in the result set.
		/// </summary>
		public uint Limit { get; set; }
	}
}