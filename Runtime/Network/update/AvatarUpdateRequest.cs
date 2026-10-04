using System;
using Nox.Avatar;
using Nox.CCK.Convertors;
using Nox.CCK.Network.Assets;

namespace Nox.Avatars.Runtime.Network {
	/// <summary>
	/// Body of <c>PATCH /avatars/{avatar}</c>: the generic asset fields. Unset values are omitted, so
	/// they keep their current value.
	/// </summary>
	[Serializable]
	public class AvatarUpdateRequest : AssetUpdateRequest {
		public static AvatarUpdateRequest From(IUpdateAvatarRequest data) {
			if (data == null)
				return new AvatarUpdateRequest();

			var request = new AvatarUpdateRequest();

			// Title: empty = no change, null = clear, other = set
			var title = data.GetTitle();
			if (!string.IsNullOrEmpty(title))
				request.Title = title.ToTranslated();
			else if (title == null)
				request.Title = null;

			var description = data.GetDescription();
			if (!string.IsNullOrEmpty(description))
				request.Description = description.ToTranslated();
			else if (description == null)
				request.Description = null;

			return request;
		}
	}
}
