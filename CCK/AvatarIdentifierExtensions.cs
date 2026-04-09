using Nox.CCK.Utils;
namespace Nox.CCK.Avatars {
	public static class AvatarIdentifierExtensions {
		public static ushort GetVersion(this Identifier identifier)
			=> identifier.Query
					.TryGetValue("v", out var v)
				&& v.Length > 0
				&& ushort.TryParse(v[0], out var ver)
					? ver
					: ushort.MaxValue;
	}
}