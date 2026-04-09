using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace Nox.Avatars {
	public interface IRuntimeAvatar {
		/// <summary>
		/// Gets the unique identifier of the avatar.
		/// </summary>
		/// <returns></returns>
		public string Id { get; }

		/// <summary>
		/// Arguments used to create the avatar.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, object> Arguments { get; }

		/// <summary>
		/// Gets the avatar descriptor, which contains metadata about the avatar.
		/// </summary>
		/// <returns></returns>
		public IAvatarDescriptor Descriptor { get; }

		/// <summary>
		/// Converts the avatar to its identifier representation.
		/// </summary>
		/// <returns></returns>
		public Identifier Identifier { get; set; }

		/// <summary>
		/// Disposes of the avatar and releases any resources it holds.
		/// </summary>
		/// <returns></returns>
		public UniTask Dispose();
	}
}