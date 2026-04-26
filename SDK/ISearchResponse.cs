using Cysharp.Threading.Tasks;

namespace Nox.Avatars {
	/// <summary>
	/// Represents the response from a search query for avatars,
	/// including pagination information and the list of avatar items.
	/// </summary>
	public interface ISearchResponse {
		/// <summary>
		/// Gets the total number of avatars that match the search query,
		/// regardless of pagination.
		/// </summary>
		public uint Total { get; }

		/// <summary>
		/// Gets the offset of the first avatar item in this response relative
		/// to the total set of matching avatars.
		/// </summary>
		public uint Offset { get; }

		/// <summary>
		/// Gets the maximum number of avatar items included in this response,
		/// which may be less than the total number of matching avatars if pagination is used.
		/// </summary>
		public uint Limit { get; }

		/// <summary>
		/// Gets the array of avatar items included in this response,
		/// which may be a subset of the total matching avatars if pagination is used.
		/// </summary>
		public IAvatar[] Items { get; }

		/// <summary>
		/// Indicates whether there are more avatar items available after the current set,
		/// based on the total, offset, and limit values.
		/// </summary>
		/// <returns></returns>
		public bool HasNext();

		/// <summary>
		/// Indicates whether there are avatar items available before the current set,
		/// based on the offset and limit values.
		/// </summary>
		/// <returns></returns>
		public bool HasPrevious();

		/// <summary>
		/// Asynchronously retrieves the next set of avatar items in the search results,
		/// if available, based on the current offset and limit values.
		/// </summary>
		/// <returns></returns>
		public UniTask<ISearchResponse> Next();

		/// <summary>
		/// Asynchronously retrieves the previous set of avatar items in the search results,
		/// if available, based on the current offset and limit values.
		/// </summary>
		/// <returns></returns>
		public UniTask<ISearchResponse> Previous();
	}
}