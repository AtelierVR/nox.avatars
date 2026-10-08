using Nox.CCK.Avatars;
using Nox.CCK.Utils;
using UnityEditor;
using UnityEngine;

namespace Nox.Avatars.Editor {
	/// <summary>
	/// Avertit l'auteur qu'un avatar contient des <see cref="Camera"/>.
	/// Les caméras d'avatar sont désactivées au chargement runtime (<c>AvatarSetup</c>) et
	/// neutralisées par le garde-fou runtime <c>CameraGuard</c> (elles ne peuvent pas rendre dans les
	/// yeux XR ni sur l'écran bureau). La notification sert de rappel côté builder.
	/// </summary>
	public static class AvatarCameraNotification {
		private const string NotificationUid = "camera_components";

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
			if (!avatar)
				return;

			var anchor = avatar.Anchor;
			if (!anchor)
				return;

			var cameras = CameraGuard.GetCameras(anchor);
			if (cameras.Length == 0)
				return;

			AvatarNotificationHelper.Set(new AvatarNotification(
				NotificationUid,
				NotificationType.Warning,
				new[] { "avatar.editor.notification.cameras", cameras.Length.ToString() },
				new AvatarAction[] {
					new(
						new[] { "avatar.editor.notification.cameras.action.untag" },
						() => {
							var av = AvatarDescriptorHelper.CurrentAvatar;
							if (!av || !av.Anchor)
								return;

							foreach (var cam in CameraGuard.GetCameras(av.Anchor)) {
								if (!cam.CompareTag("MainCamera") && cam.stereoTargetEye == StereoTargetEyeMask.None)
									continue;

								Undo.RecordObject(cam.gameObject, "Untag Cameras");
								Undo.RecordObject(cam, "Untag Cameras");
								CameraGuard.Demote(cam);
							}

							OnHierarchyChanged();
						}
					)
				}
			));
		}
	}
}
