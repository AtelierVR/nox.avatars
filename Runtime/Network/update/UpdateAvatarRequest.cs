using Newtonsoft.Json.Linq;
using Nox.Avatar;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	[System.Serializable]
	public class UpdateAvatarRequest : IUpdateAvatarRequest, INoxObject {
		public string title       = "";
		public string description = "";
		public string thumbnail   = "";

		public IUpdateAvatarRequest SetTitle(string t) {
			title = t;
			return this;
		}

		public IUpdateAvatarRequest SetDescription(string d) {
			description = d;
			return this;
		}


		public IUpdateAvatarRequest SetThumbnail(string i) {
			thumbnail = i;
			return this;
		}

		public string GetTitle()
			=> title;

		public string GetDescription()
			=> description;

		public string GetThumbnail()
			=> thumbnail;

		public string ToJson() {
			var obj = new JObject();

			if (title == null)
				obj["title"] = JValue.CreateNull();
			else if (title.Length > 0)
				obj["title"] = JValue.CreateString(title);

			if (description == null)
				obj["description"] = JValue.CreateNull();
			else if (description.Length > 0)
				obj["description"] = JValue.CreateString(description);

			if (thumbnail == null)
				obj["thumbnail"] = JValue.CreateNull();
			else if (thumbnail.Length > 0)
				obj["thumbnail"] = JValue.CreateString(thumbnail);

			return obj.ToString();
		}

		public static UpdateAvatarRequest FromBase(IUpdateAvatarRequest form)
			=> new() {
				title       = form.GetTitle(),
				description = form.GetDescription(),
				thumbnail   = form.GetThumbnail()
			};
	}
}