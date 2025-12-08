namespace Nox.Avatars {
	public interface ISearchRequest {
		public ISearchRequest SetQuery(string query);

		public ISearchRequest SetIds(uint[] userIds);

		public ISearchRequest SetOffset(uint offset);

		public ISearchRequest SetLimit(uint limit);

		public string GetQuery();

		public uint[] GetIds();

		public uint GetOffset();

		public uint GetLimit();
	}
}