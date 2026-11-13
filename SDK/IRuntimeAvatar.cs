using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Utils;

namespace Nox.Avatars {
	public interface IRuntimeAvatar {
		/// <summary>
		/// Name of the event sent on <see cref="Bus"/> once the saved data of the avatar was read: an avatar is
		/// identified only once it is loaded, so a module often runs before its data is available.
		/// </summary>
		public const string DataLoadedEvent = "data.loaded";

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
		/// Bus every module of this avatar shares: a module sends an event on it and all the modules of the
		/// avatar receive it (see <see cref="IAvatarModule.OnBusEvent"/>).
		/// </summary>
		public IAvatarModuleBus Bus { get; }

		/// <summary>
		/// Converts the avatar to its identifier representation.
		/// </summary>
		/// <returns></returns>
		public Identifier Identifier { get; set; }

		/// <summary>
		/// Identifier of the user the avatar is loaded for (its local player), or
		/// <see cref="Identifier.Invalid"/> when unknown. The saved parameter profiles are scoped by it, and
		/// by <see cref="Identifier"/>.
		/// </summary>
		public Identifier Owner { get; set; }

		/// <summary>
		/// Whether the avatar is the local player's. Only a local avatar persists anything: a remote one
		/// keeps <see cref="Data"/> in memory.
		/// </summary>
		public bool IsLocal { get; }

		/// <summary>
		/// Persisted values of the avatar (the saved parameter profiles, see
		/// <c>AvatarParameterProfiles</c>). Never null: <see cref="ISavableContainer.Load"/> fills it from its
		/// storage and <see cref="ISavableContainer.Save"/> writes it back.
		/// </summary>
		public ISavableContainer Data { get; }

		/// <summary>
		/// Cancelled when the avatar goes away (see <see cref="Dispose"/>). Pass it to everything started for
		/// this avatar (reading its saved data, deferred saves, loads…) so the work stops with it instead of
		/// running against a half-destroyed avatar.
		/// </summary>
		public CancellationToken Token { get; }

		/// <summary>
		/// Disposes of the avatar and releases any resources it holds.
		/// </summary>
		/// <returns></returns>
		public UniTask Dispose();
	}
}
