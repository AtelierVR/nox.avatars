using Nox.Assets.Editor;
using Nox.CCK.Avatars;
using UnityEditor;

namespace Nox.Avatars.Editor {
	/// <summary>
	/// Runs the notifications shared by every asset kind (<see cref="AssetDescriptorNotifications"/>:
	/// cameras, mesh import settings, missing components) on the selected avatar, and declares the avatar
	/// to <see cref="AssetPlayModeNotifications"/> — an avatar is built from edit mode only, so entering
	/// Play mode is reported as an error.
	/// </summary>
	public static class AvatarDescriptorNotifications {
		/// <summary>Panel id of the avatar publisher/builder (<c>AvatarBuilderSource.Panel</c>).</summary>
		private const string Kind = "avatar";

		[InitializeOnLoadMethod]
		private static void Initialize() {
			AssetPlayModeNotifications.Register(Kind);

			AvatarDescriptorHelper.OnAvatarSelected.AddListener(OnAvatarSelected);
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			OnAvatarSelected(AvatarDescriptorHelper.CurrentAvatar);
		}

		private static void OnHierarchyChanged()
			=> OnAvatarSelected(AvatarDescriptorHelper.CurrentAvatar);

		private static void OnAvatarSelected(AvatarDescriptor avatar) {
			if (!avatar) {
				AssetDescriptorNotifications.Clear(Kind);
				return;
			}

			// The root is read on every call, actions included: a fix must run on the avatar selected
			// when the user clicks it, not on the one that was selected when the notification was made.
			AssetDescriptorNotifications.Update(Kind, CurrentRoot);
		}

		private static UnityEngine.GameObject CurrentRoot() {
			var avatar = AvatarDescriptorHelper.CurrentAvatar;
			return avatar && avatar.Anchor ? avatar.Anchor : null;
		}
	}
}
