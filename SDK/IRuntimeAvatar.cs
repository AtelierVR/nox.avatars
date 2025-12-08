using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.Avatars {
	public interface IRuntimeAvatar {
		/// <summary>
		/// Gets the unique identifier of the avatar.
		/// </summary>
		/// <returns></returns>
		public string GetId();

		/// <summary>
		/// Arguments used to create the avatar.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, object> GetArguments();

		/// <summary>
		/// Gets the avatar descriptor, which contains metadata about the avatar.
		/// </summary>
		/// <returns></returns>
		public IAvatarDescriptor GetDescriptor();

		/// <summary>
		/// Converts the avatar to its identifier representation.
		/// </summary>
		/// <returns></returns>
		public IAvatarIdentifier GetIdentifier();

		/// <summary>
		/// Sets the avatar's identifier.
		/// </summary>
		/// <param name="identifier"></param>
		public void SetIdentifier(IAvatarIdentifier identifier);

		/// <summary>
		/// Disposes of the avatar and releases any resources it holds.
		/// </summary>
		/// <returns></returns>
		public UniTask Dispose();
	}
}