using Nox.Avatars.Runtime.client;
using Cysharp.Threading.Tasks;
using Nox.Search;
using UnityEngine;

namespace Nox.Avatars.Runtime.Search {
	public class SearchData : IResultData {
		public Network.Avatar Reference;

		public int Id
			=> Reference.GetIdentifier().GetHashCode();

		public string[] TitleArguments
			=> new[] { Reference.GetTitle() ?? Reference.GetIdentifier().ToString() };

		public UniTask<Texture2D> Image
			=> Main.NetworkAPI.FetchTexture(Reference.GetThumbnailUrl());

		public void OnClick(int menuId)
			=> Client.UiAPI?.SendGoto(menuId, AvatarPage.GetStaticKey(), "avatar", Reference);
	}
}