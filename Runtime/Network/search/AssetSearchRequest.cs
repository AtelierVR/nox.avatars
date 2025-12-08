using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	public class AssetSearchRequest : IAssetSearchRequest, INoxObject {
		public uint     Offset;
		public uint     Limit;
		public bool     ShowEmpty;
		public ushort[] Versions;
		public string[] Engines;
		public string[] Platforms;

		public string ToParams() {
			var text             = "";
			if (Offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
			if (Limit  > 0) text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
			if (ShowEmpty) text  += (text.Length > 0 ? "&" : "") + "empty";
			if (Versions != null)
				foreach (var v in Versions.Where(v => v != ushort.MaxValue))
					text += (text.Length                > 0 ? "&" : "") + $"version={v}";
			if (Engines != null)
				foreach (var e in Engines)
					text += (text.Length > 0 ? "&" : "") + $"engine={e}";
			if (Platforms != null)
				foreach (var p in Platforms)
					text += (text.Length > 0 ? "&" : "") + $"platform={p}";
			return string.IsNullOrEmpty(text) ? "" : "?" + text;
		}

		public IAssetSearchRequest SetOffset(uint o) {
			Offset = o;
			return this;
		}

		public IAssetSearchRequest SetLimit(uint l) {
			Limit = l;
			return this;
		}

		public IAssetSearchRequest SetShowEmpty(bool showEmpty) {
			ShowEmpty = showEmpty;
			return this;
		}

		public IAssetSearchRequest SetVersions(ushort[] v) {
			Versions = v;
			return this;
		}

		public IAssetSearchRequest SetEngines(string[] e) {
			Engines = e;
			return this;
		}

		public IAssetSearchRequest SetPlatforms(string[] p) {
			Platforms = p;
			return this;
		}

		public uint GetOffset()
			=> Offset;

		public uint GetLimit()
			=> Limit;

		public bool GetShowEmpty()
			=> ShowEmpty;

		public ushort[] GetVersions()
			=> Versions ?? Array.Empty<ushort>();

		public string[] GetEngines()
			=> Engines ?? Array.Empty<string>();

		public string[] GetPlatforms()
			=> Platforms ?? Array.Empty<string>();

		public static AssetSearchRequest FromBase(IAssetSearchRequest data)
			=> new() {
				Offset    = data.GetOffset(),
				Limit     = data.GetLimit(),
				ShowEmpty = data.GetShowEmpty(),
				Versions  = data.GetVersions(),
				Engines   = data.GetEngines(),
				Platforms = data.GetPlatforms()
			};
	}
}