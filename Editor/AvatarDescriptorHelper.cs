using Nox.CCK.Avatars;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Editor {
	public class AvatarDescriptorHelper {
		public static AvatarDescriptor CurrentAvatar;

		public static readonly UnityEvent<AvatarDescriptor> OnAvatarSelected = new();

		[InitializeOnLoadMethod]
		private static void Initialize() {
			Selection.selectionChanged                    += OnSelectionChanged;
			EditorApplication.hierarchyChanged            += Find;
			EditorSceneManager.sceneOpened                += (_, _) => ForceFind();
			EditorSceneManager.activeSceneChangedInEditMode += (_, _) => ForceFind();
			Find();
		}

		private static void OnSelectionChanged() {
			if (!Selection.activeGameObject) return;
			var avatarDescriptor = Selection.activeGameObject.GetComponent<AvatarDescriptor>();
			if (!avatarDescriptor)
				avatarDescriptor = Selection.activeGameObject.GetComponentInParent<AvatarDescriptor>();
			if (avatarDescriptor && avatarDescriptor != CurrentAvatar)
				SetCurrentAvatar(avatarDescriptor);
		}

		public static void ForceFind() {
			SetCurrentAvatar(null);
			Find();
		}

		public static void Find() {
			try {
				// Vérifier si CurrentAvatar est null, Missing ou valide et actif
				if (CurrentAvatar != null && CurrentAvatar && CurrentAvatar.gameObject.activeInHierarchy) return;
				var activeAvatars = Object.FindObjectsByType<AvatarDescriptor>(FindObjectsSortMode.None)
					.Where(avatar => avatar.gameObject.activeInHierarchy)
					.ToArray();
				SetCurrentAvatar(activeAvatars.Length > 0 ? activeAvatars[0] : null);
			} catch {
				SetCurrentAvatar(null);
			}
		}

		public static void SetCurrentAvatar(AvatarDescriptor newAvatar) {
			if (ReferenceEquals(CurrentAvatar, newAvatar)) return;
			Logger.LogDebug($"Current avatar changed to {(newAvatar ? newAvatar.name : "null")}");
			CurrentAvatar = newAvatar;
			OnAvatarSelected?.Invoke(newAvatar);
		}
	}
}