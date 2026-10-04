using Nox.CCK.Utils;

namespace Nox.CCK.Avatars {
	public static class AvatarIdentifierExtensions {
		public const string AvatarType = "a";

		public const string VersionQuery = "v";

		public const ushort DefaultVersion = ushort.MaxValue;

		public static ushort GetVersion(this Identifier identifier)
			=> identifier.IsValid()
				&& identifier.Query.TryGetValue(VersionQuery, out var v)
				&& v.Length > 0
				&& ushort.TryParse(v[0], out var version)
					? version
					: DefaultVersion;
	}
}
