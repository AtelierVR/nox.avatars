using System;
using Newtonsoft.Json;
using Nox.CCK.Network.Assets;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	/// <summary>
	/// An avatar as returned by the avatars endpoint: the generic <see cref="Asset"/> narrowed to the
	/// <see cref="IAvatar"/> type. Avatars currently carry no field of their own.
	/// </summary>
	[Serializable, JsonObject]
	public class Avatar : Asset, IAvatar, INoxObject {
		/// <summary>Canonical reference of the avatar (<c>a:&lt;id&gt;@&lt;server&gt;</c>).</summary>
		[JsonIgnore]
		public override Identifier Identifier
			=> new("a", Id, null, Server);

		public override string ToString()
			=> $"{GetType().Name}[id={Id}, name={Name ?? "<no-name>"}, owner={Owner}, server={Server}, images={Images?.Length ?? 0}]";
	}
}
