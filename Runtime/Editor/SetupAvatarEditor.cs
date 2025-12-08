using UnityEngine;
using UnityEditor;
using Nox.CCK.Avatars;
using Nox.CCK.Language;

namespace Nox.Avatars.Runtime.Editor {
	[CustomEditor(typeof(Animator))]
	public class SetupAvatarEditor : UnityEditor.Editor {
		public static bool IsHuman(Animator animator)
			=> animator && animator.avatar && animator.avatar.isHuman;

		public override void OnInspectorGUI() {
			var animator = (Animator)target;

			if (IsHuman(animator) && !animator.GetComponent<AvatarDescriptor>()) {
				EditorGUILayout.HelpBox(LanguageManager.Get("nox.avatar.no_avatar_descriptor"), MessageType.Info);
				if (GUILayout.Button(LanguageManager.Get("nox.avatar.add_avatar_descriptor")))
					MakeDescriptor(animator);
				EditorGUILayout.Space();
			}

			base.OnInspectorGUI();
		}

		private static void MakeDescriptor(Animator animator) {
			var descriptor = animator.gameObject.AddComponent<AvatarDescriptor>();
			EditorUtility.SetDirty(descriptor);
		}
	}
}