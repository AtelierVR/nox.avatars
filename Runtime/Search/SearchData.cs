using Nox.Avatars.Runtime.client;
using Cysharp.Threading.Tasks;
using Nox.CCK.Convertors;
using Nox.CCK.Network.Assets;
using Nox.Search;

namespace Nox.Avatars.Runtime.Search {
	public class SearchData : IResultData {
		public Network.Avatar Reference;

		public int Id
			=> Reference.Identifier.GetHashCode();

		public string[] TitleArguments
			=> new[] { Reference.Title?.Resolve() ?? Reference.Identifier.ToString() };

		public UniTask<ImageSource> Image
			=> UniTask.FromResult(ImageSource.FromUrl(Reference.BestImage(4f / 3f)?.Url));

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, AvatarPage.GetStaticKey(), "avatar", Reference);
	}
}