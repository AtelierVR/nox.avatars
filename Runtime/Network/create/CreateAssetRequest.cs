using Newtonsoft.Json.Linq;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	public class CreateAssetRequest : ICreateAssetRequest, INoxObject {
		public uint   ID;
		public ushort Version;
		public string Engine;
		public string Platform;
		public string URL;
		public string Hash;
		public long   Size;

		public ICreateAssetRequest SetId(uint id) {
			ID = id;
			return this;
		}

		public ICreateAssetRequest SetVersion(ushort version) {
			Version = version;
			return this;
		}

		public ICreateAssetRequest SetEngine(string engine) {
			Engine = engine;
			return this;
		}

		public ICreateAssetRequest SetPlatform(string platform) {
			Platform = platform;
			return this;
		}

		public ICreateAssetRequest SetUrl(string url) {
			URL = url;
			return this;
		}

		public ICreateAssetRequest SetHash(string hash) {
			Hash = hash;
			return this;
		}

		public ICreateAssetRequest SetSize(long size) {
			Size = size;
			return this;
		}

		public uint GetId()
			=> ID;

		public ushort GetVersion()
			=> Version;

		public string GetEngine()
			=> Engine;

		public string GetPlatform()
			=> Platform;

		public string GetUrl()
			=> URL;

		public string GetHash()
			=> Hash;

		public long GetSize()
			=> Size;

		public string ToJson() {
			var obj = new JObject {
				["version"]  = Version,
				["engine"]   = Engine,
				["platform"] = Platform
			};

			if (ID   > 0) obj["id"]   = ID;
			if (Size > 0) obj["size"] = Size;

			if (!string.IsNullOrEmpty(Engine))
				obj["engine"] = Engine;

			if (!string.IsNullOrEmpty(Platform))
				obj["platform"] = Platform;

			if (!string.IsNullOrEmpty(URL))
				obj["url"] = URL;

			if (!string.IsNullOrEmpty(Hash))
				obj["hash"] = Hash;

			return obj.ToString();
		}

		public static CreateAssetRequest FromBase(ICreateAssetRequest data)
			=> new() {
				ID       = data.GetId(),
				Version  = data.GetVersion(),
				Engine   = data.GetEngine(),
				Platform = data.GetPlatform(),
				URL      = data.GetUrl(),
				Hash     = data.GetHash(),
				Size     = data.GetSize()
			};
	}
}