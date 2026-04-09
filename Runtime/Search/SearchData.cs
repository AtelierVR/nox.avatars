using Nox.Avatars.Runtime.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;

namespace Nox.Avatars.Runtime.Search {
	public class SearchData : IResultData {
		public Network.Avatar Reference;

		public int Id
			=> Reference.Identifier.GetHashCode();

		public string[] TitleArguments
			=> new[] { Reference.Title ?? Reference.Identifier.ToString() };

		public UniTask<Texture2D> Image
			=> Main.NetworkAPI.FetchTexture(Reference.Thumbnail);

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, AvatarPage.GetStaticKey(), "avatar", Reference);
	}
}