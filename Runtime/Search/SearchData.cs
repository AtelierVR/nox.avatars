using Nox.Avatars.Runtime.client;
using Cysharp.Threading.Tasks;
using Nox.Search;

namespace Nox.Avatars.Runtime.Search {
	public class SearchData : IResultData {
		public Network.Avatar Reference;

		public int Id
			=> Reference.Identifier.GetHashCode();

		public string[] TitleArguments
			=> new[] { Reference.Title ?? Reference.Identifier.ToString() };

		public UniTask<ImageSource> Image
			=> UniTask.FromResult(ImageSource.FromUrl(Reference.Thumbnail));

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, AvatarPage.GetStaticKey(), "avatar", Reference);
	}
}