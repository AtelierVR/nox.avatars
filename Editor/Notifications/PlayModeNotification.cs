using Nox.Assets.Editor;
using UnityEditor;

namespace Nox.Avatars.Editor {
	public static class PlayModeNotification {
		private const string NotificationUid = "play_mode";

		[InitializeOnLoadMethod]
		private static void Initialize()
			=> EditorApplication.playModeStateChanged += OnPlayMode;

		private static void OnPlayMode(PlayModeStateChange state) {
			if (state == PlayModeStateChange.ExitingEditMode && !AssetNotificationHelper.Has(NotificationUid))
				AssetNotificationHelper.Add(
					new AssetNotification(
						NotificationUid,
						AssetNotificationType.Error,
						new[] { "avatar.editor.notification.play_mode" }
					)
				);

			if (state == PlayModeStateChange.EnteredEditMode)
				AssetNotificationHelper.Remove(NotificationUid);
		}
	}
}