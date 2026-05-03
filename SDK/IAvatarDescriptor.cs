using UnityEngine;

namespace Nox.Avatars {
	public interface IAvatarDescriptor {
		/// <summary>
		/// Get the root GameObject of the avatar.
		/// </summary>
		/// <returns></returns>
		public GameObject Anchor { get; }

		/// <summary>
		/// Gets the avatar modules of a specific type.
		/// Is used to retrieve modules that implement a specific interface.
		/// Like Voice, Eyes, etc.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T[] GetModules<T>() where T : IAvatarModule;

		/// <summary>
		/// Gets all avatar modules.
		/// </summary>
		/// <returns></returns>
		public IAvatarModule[] Modules { get; }

		/// <summary>
		/// Updates all avatar modules.
		/// </summary>
		public IAvatarModule[] RefreshModules();

		/// <summary>
		/// Gets the animator component associated with this avatar descriptor.
		/// </summary>
		/// <returns></returns>
		public Animator Animator { get; }
	}
}