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

			var avatars    = Object.FindObjectsByType<AvatarDescriptor>(FindObjectsSortMode.None);
			var humanoids  = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
			var candidates = System.Array.FindAll(humanoids, a => a.avatar != null && a.avatar.isHuman);

			AvatarNotificationHelper.Set(
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
							)
						}
						: candidates.Length > 0
							? new AvatarAction[] {
								new(
									new[] { "avatar.editor.notification.no_avatar_descriptor.action.select_humanoids" },
									() => {
										var objects = new Object[candidates.Length];
										for (var i = 0; i < candidates.Length; i++)
											objects[i] = candidates[i].gameObject;
										Selection.objects = objects;
									}
								)
							}
							: System.Array.Empty<AvatarAction>()
				)
			);
		}
	}
}