using Nox.Avatars.Editor;
using Nox.CCK.Mods.Events;
using Nox.Users;

namespace Nox.Avatars.Runtime.Editor {
	public class UserConnectedNotification {
		private const string NotConnectedUid = "not_connected";
		private const string ConnectedUid    = "connected";

		public static void OnUserUpdated(EventData context)
			=> OnUserUpdated(context.TryGet(0, out ICurrentUser u) ? u : null);

		public static void OnUserUpdated(ICurrentUser user) {
			if (user == null)
				UpdateNotConnected();
			else UpdateConnected(user);
		}

		private static void UpdateNotConnected() {
			AvatarNotificationHelper.Remove(ConnectedUid);
			if (AvatarNotificationHelper.Has(NotConnectedUid)) return;
			var notification = new AvatarNotification(
				NotConnectedUid,
				NotificationType.Warning,
				new[] { "avatar.editor.notification.user_not_connected" }
			);
			AvatarNotificationHelper.Add(notification);
		}

		private static void UpdateConnected(ICurrentUser user) {
			AvatarNotificationHelper.Remove(NotConnectedUid);
			if (AvatarNotificationHelper.Has(ConnectedUid)) return;
			var notification = new AvatarNotification(
				ConnectedUid,
				NotificationType.Info,
				new[] {
					"avatar.editor.notification.user_connected",
					user.GetDisplay(),
					user.ToIdentifier().ToString()
				}
			);
			AvatarNotificationHelper.Add(notification);
		}
	}
}