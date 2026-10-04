using Nox.Network.Assets;

namespace Nox.Avatars {
	/// <summary>
	/// The avatars collection: declared server-side with <c>@AssetController('avatar', 'avatars')</c>,
	/// so every route is the generic asset route applied to <c>avatars</c>.
	/// </summary>
	public static class AvatarsEndpoint {
		/// <summary>Logical asset type expected by the server-side validator.</summary>
		public const string Type = "avatar";

		/// <summary>REST endpoint holding the collection, relative to the node gateway.</summary>
		public const string Route = "avatars";

		public static AssetEndpoint Endpoint
			=> new(Type, Route);
	}
}
