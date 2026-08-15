namespace Nox.Avatars {
	public interface IAssetSearchRequest {
		/// <summary>
		/// The offset for the search results.
		/// </summary>
		/// <returns></returns>
		public uint Offset { get; set; }

		/// <summary>
		/// The limit for the search results.
		/// </summary>
		/// <returns></returns>
		public uint Limit { get; set; }

		/// <summary>
		/// The flag indicating if the search results should include empty assets.
		/// </summary>
		/// <returns></returns>
		public bool ShowEmpty { get; set; }

		/// <summary>
		/// The versions to filter the search results.
		/// </summary>
		/// <returns></returns>
		public ushort[] Versions { get; set; }

		/// <summary>
		/// The engines to filter the search results.
		/// </summary>
		/// <returns></returns>
		public string[] Engines { get; set; }

		/// <summary>
		/// The platforms to filter the search results.
		/// </summary>
		/// <returns></returns>
		public string[] Platforms { get; set; }
	}
}