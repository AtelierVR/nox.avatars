using Nox.CCK.Avatars;
using UnityEditor;
using UnityEngine;

namespace Nox.Avatars.Editor {
	public static class AvatarAnimatorNotification {
		private const string NoAnimatorUid  = "no_animator";
		private const string NotHumanoidUid = "not_humanoid";

		[InitializeOnLoadMethod]
		private static void OnInitialize() {
			AvatarDescriptorHelper.OnAvatarSelected.AddListener(OnAvatarSelected);
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			OnAvatarSelected(AvatarDescriptorHelper.CurrentAvatar);
		}

		private static void OnHierarchyChanged()
			=> OnAvatarSelected(AvatarDescriptorHelper.CurrentAvatar);

		private static void OnAvatarSelected(AvatarDescriptor avatar) {
			AvatarNotificationHelper.Remove(NoAnimatorUid);
			AvatarNotificationHelper.Remove(NotHumanoidUid);
			if (!avatar) return;

			var animator = avatar.GetComponent<Animator>();
			if (!animator) {
				AvatarNotificationHelper.Set(new AvatarNotification(
					NoAnimatorUid,
					NotificationType.Error,
					new[] { "avatar.editor.notification.no_animator" }
				));
				return;
			}

			if (animator.avatar == null || !animator.avatar.isHuman)
				AvatarNotificationHelper.Set(new AvatarNotification(
					NotHumanoidUid,
					NotificationType.Warning,
					new[] { "avatar.editor.notification.not_humanoid" },
					new AvatarAction[] {
						new(
							new[] { "avatar.editor.notification.not_humanoid.action.select" },
							() => {
								var av = AvatarDescriptorHelper.CurrentAvatar;
								if (av) Selection.activeGameObject = av.gameObject;
							}
						)
					}
				));
		}
	}
}
