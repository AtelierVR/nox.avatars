using Newtonsoft.Json.Linq;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	public class CreateAvatarRequest : ICreateAvatarRequest, INoxObject {
		public uint   Id;
		public string Title;
		public string Description;
		public string Thumbnail;

		public ICreateAvatarRequest SetId(uint i) {
			Id = i;
			return this;
		}

		public ICreateAvatarRequest SetTitle(string t) {
			Title = t;
			return this;
		}

		public ICreateAvatarRequest SetDescription(string d) {
			Description = d;
			return this;
		}

		public ICreateAvatarRequest SetThumbnail(string t) {
			Thumbnail = t;
			return this;
		}

		public uint GetId()
			=> Id;

		public string GetTitle()
			=> Title;

		public string GetDescription()
			=> Description;

		public string GetThumbnail()
			=> Thumbnail;

		public string ToJson() {
			var obj = new JObject();

			if (Id > 0) obj["id"] = Id;

			if (!string.IsNullOrEmpty(Title))
				obj["title"] = Title;

			if (!string.IsNullOrEmpty(Description))
				obj["description"] = Description;

			if (!string.IsNullOrEmpty(Thumbnail))
				obj["thumbnail"] = Thumbnail;

			return obj.ToString();
		}

		public static CreateAvatarRequest FromBase(ICreateAvatarRequest data)
			=> new() {
				Id           = data.GetId(),
				Title       = data.GetTitle(),
				Description = data.GetDescription(),
				Thumbnail   = data.GetThumbnail()
			};
	}
}