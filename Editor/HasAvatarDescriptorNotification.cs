using Nox.CCK.Avatars;
using UnityEditor;
using UnityEngine;

namespace Nox.Avatars.Editor {
	public static class HasAvatarDescriptorNotification {
		private const string NotificationUid = "has_avatar_descriptor";

		[InitializeOnLoadMethod]
		private static void OnInitialize() {
			AvatarDescriptorHelper.OnAvatarSelected.AddListener(OnAvatarSelected);
			OnAvatarSelected(AvatarDescriptorHelper.CurrentAvatar);
		}

		private static void OnAvatarSelected(AvatarDescriptor avatar) {
			if (avatar) {
				AvatarNotificationHelper.Remove(NotificationUid);
				return;
			}

			var avatars = Object.FindObjectsByType<AvatarDescriptor>(FindObjectsSortMode.None);

			AvatarNotificationHelper.Add(
				new AvatarNotification(
					NotificationUid,
					NotificationType.Warning,
					avatars.Length > 0
						? new[] { "avatar.editor.notification.no_avatar_descriptor.selected" }
						: new[] { "avatar.editor.notification.no_avatar_descriptor.found" },
					avatars.Length > 0
						? new AvatarAction[] {
							new(
								new[] { "avatar.editor.notification.no_avatar_descriptor.action.select_first" },
								() => Selection.activeGameObject = avatars[0].gameObject
							),
							new(
								new[] { "avatar.editor.notification.no_avatar_descriptor.action.create_new" },
								() => Selection.activeGameObject = new GameObject("AvatarDescriptor", typeof(AvatarDescriptor))
							)
						}
						: new AvatarAction[] {
							new(
								new[] { "avatar.editor.notification.no_avatar_descriptor.action.create" },
								() => Selection.activeGameObject = new GameObject("AvatarDescriptor", typeof(AvatarDescriptor))
							)
						}
				)
			);
		}
	}
}