using System;
using Nox.CCK.Convertors;
using Nox.CCK.Network.Assets;

namespace Nox.Avatars.Runtime.Network {
	/// <summary>
	/// Body of <c>PUT /avatars</c>: the generic asset fields. Unset values are omitted so the server
	/// keeps its defaults.
	/// </summary>
	[Serializable]
	public class AvatarCreateRequest : AssetCreateRequest {
		public static AvatarCreateRequest From(ICreateAvatarRequest data) {
			if (data == null)
				return new AvatarCreateRequest();

			var request = new AvatarCreateRequest { Id = data.GetId() };

			if (!string.IsNullOrEmpty(data.GetTitle()))
				request.Title = data.GetTitle().ToTranslated();

			if (!string.IsNullOrEmpty(data.GetDescription()))
				request.Description = data.GetDescription().ToTranslated();

			return request;
		}
	}
}
