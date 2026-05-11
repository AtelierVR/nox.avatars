using Nox.CCK.Avatars;
using UnityEditor;
using UnityEngine;

namespace Nox.Avatars.Editor {
	public static class AvatarMissingComponentsNotification {
		private const string NotificationUid = "missing_components";

		[InitializeOnLoadMethod]
		private static void OnInitialize() {
			AvatarDescriptorHelper.OnAvatarSelected.AddListener(OnAvatarSelected);
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			OnAvatarSelected(AvatarDescriptorHelper.CurrentAvatar);
		}

		private static void OnHierarchyChanged()
			=> OnAvatarSelected(AvatarDescriptorHelper.CurrentAvatar);

		private static void OnAvatarSelected(AvatarDescriptor avatar) {
			AvatarNotificationHelper.Remove(NotificationUid);
			if (!avatar) return;

			var count = 0;
			foreach (var t in avatar.GetComponentsInChildren<Transform>(true))
				count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);

			if (count == 0) return;

			AvatarNotificationHelper.Set(new AvatarNotification(
				NotificationUid,
				NotificationType.Warning,
				new[] { "avatar.editor.notification.missing_components", count.ToString() },
				new AvatarAction[] {
					new(
						new[] { "avatar.editor.notification.missing_components.action.remove" },
						() => {
							var av = AvatarDescriptorHelper.CurrentAvatar;
							if (!av) return;
							Undo.RegisterFullObjectHierarchyUndo(av.gameObject, "Remove Missing Scripts");
							foreach (var t in av.GetComponentsInChildren<Transform>(true))
								GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
							OnHierarchyChanged();
						}
					)
				}
			));
		}
	}
}
