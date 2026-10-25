using Nox.Assets.Editor;
using Nox.CCK.Avatars;
using UnityEditor;
using UnityEngine;

namespace Nox.Avatars.Editor {
	public class ChildrenAvatarNotification {
		public const string Uid = "children_avatars";

		[InitializeOnLoadMethod]
		private static void Initialize() {
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			OnHierarchyChanged();
		}

		private static void OnHierarchyChanged() {
			var currentAvatar = AvatarDescriptorHelper.CurrentAvatar;
			if (!currentAvatar) {
				AssetNotificationHelper.Remove(Uid);
				return;
			}

			var childrenCount = 0;
			foreach (Transform child in currentAvatar.transform)
				if (child.GetComponent<AvatarDescriptor>())
					childrenCount++;

			if (childrenCount == 0) {
				AssetNotificationHelper.Remove(Uid);
				return;
			}

			AssetNotificationHelper.Set(
				new AssetNotification(
					Uid,
					AssetNotificationType.Warning,
					new[] { "avatar.editor.notification.children_avatars", childrenCount.ToString() },
					new[] {
						new AssetNotificationAction(
							new[] { "avatar.editor.notification.children_avatars.action.select_children" },
							() => {
								var c = AvatarDescriptorHelper.CurrentAvatar;
								if (!c) return;
								var childAvatars = new System.Collections.Generic.List<Object>();

								foreach (Transform child in c.transform) {
									var childAvatar = child.GetComponent<AvatarDescriptor>();
									if (childAvatar)
										childAvatars.Add(childAvatar.gameObject);
								}

								Selection.objects = childAvatars.ToArray();
							}
						)
					}
				)
			);
		}
	}
}