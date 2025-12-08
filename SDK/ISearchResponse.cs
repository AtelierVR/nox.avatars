using Cysharp.Threading.Tasks;

namespace Nox.Avatars {
	public interface ISearchResponse {
		public string GetQuery();
		public uint[] GetIds();

		public IAvatar[] GetAvatars();
		public uint      GetTotal();
		public uint      GetLimit();
		public uint      GetOffset();
		public bool      HasNext();
		public bool      HasPrevious();

		public UniTask<ISearchResponse> Next();
		public UniTask<ISearchResponse> Previous();
	}
}