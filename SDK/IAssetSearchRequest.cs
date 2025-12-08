namespace Nox.Avatars {
	public interface IAssetSearchRequest {
		/// <summary>
		/// Set the offset for the search results.
		/// Zero means no offset.
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public IAssetSearchRequest SetOffset(uint o);

		/// <summary>
		/// Set the limit for the search results.
		/// The minimum is 1 and the maximum is defined by the server.
		/// </summary>
		/// <param name="l"></param>
		/// <returns></returns>
		public IAssetSearchRequest SetLimit(uint l);

		/// <summary>
		/// Sets if you want to search for assets that are empty.
		/// </summary>
		/// <param name="showEmpty"></param>
		/// <returns></returns>
		public IAssetSearchRequest SetShowEmpty(bool showEmpty);

		/// <summary>
		/// Sets the versions to filter the search results.
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		public IAssetSearchRequest SetVersions(ushort[] v);

		/// <summary>
		/// Sets the engines to filter the search results.
		/// See <see cref="Nox.CCK.Utils.Engine"/> for more information.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public IAssetSearchRequest SetEngines(string[] e);

		/// <summary>
		/// Sets the platforms to filter the search results.
		/// See <see cref="Nox.CCK.Utils.Platform"/> for more information.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public IAssetSearchRequest SetPlatforms(string[] p);

		/// <summary>
		/// Gets the offset for the search results.
		/// </summary>
		/// <returns></returns>
		public uint GetOffset();

		/// <summary>
		/// Gets the limit for the search results.
		/// </summary>
		/// <returns></returns>
		public uint GetLimit();

		/// <summary>
		/// Gets if the search results should include empty assets.
		/// </summary>
		/// <returns></returns>
		public bool GetShowEmpty();

		/// <summary>
		/// Gets the versions to filter the search results.
		/// </summary>
		/// <returns></returns>
		public ushort[] GetVersions();

		/// <summary>
		/// Gets the engines to filter the search results.
		/// </summary>
		/// <returns></returns>
		public string[] GetEngines();

		/// <summary>
		/// Gets the platforms to filter the search results.
		/// </summary>
		/// <returns></returns>
		public string[] GetPlatforms();
	}
}