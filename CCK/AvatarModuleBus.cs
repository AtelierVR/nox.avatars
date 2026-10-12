using Nox.CCK.Utils;

namespace Nox.Avatars {
	/// <summary>
	/// Bus of one avatar (see <see cref="IRuntimeAvatar.Bus"/>): <see cref="Send"/> hands an event to every
	/// module of the avatar's descriptor, so all the modules of an avatar share it and two avatars never see
	/// each other's events.
	/// </summary>
	public sealed class AvatarModuleBus : IAvatarModuleBus {
		private readonly IRuntimeAvatar _avatar;

		public AvatarModuleBus(IRuntimeAvatar avatar)
			=> _avatar = avatar;

		public void Send(string name, IBusContext context = null) {
			if (string.IsNullOrEmpty(name))
				return;

			// Resolved on every send: the avatar may not have its descriptor yet when it hands out its bus.
			var modules = _avatar != null ? _avatar.Descriptor?.Modules : null;
			if (modules == null)
				return;

			foreach (var module in modules) {
				// A destroyed Unity object is not null: skip it rather than send it the event.
				if (module is UnityEngine.Object mo && mo.IsDestroyed())
					continue;

				module?.OnBusEvent(name, context);
			}
		}
	}
}
