using System.Collections.Generic;
using Nox.CCK.Avatars;
using UnityEditor;
using UnityEngine;

namespace Nox.Avatars.Editor {
	public static class AvatarMeshSettingsNotification {
		[InitializeOnLoadMethod]
		private static void OnInitialize() {
			AvatarDescriptorHelper.OnAvatarSelected.AddListener(OnAvatarSelected);
			OnAvatarSelected(AvatarDescriptorHelper.CurrentAvatar);
		}

		private static void OnAvatarSelected(AvatarDescriptor avatar) {
			AvatarNotificationHelper.Remove("mesh.rw.*");
			AvatarNotificationHelper.Remove("mesh.lbsn.*");
			if (!avatar) return;

			var processed = new HashSet<string>();

			foreach (var smr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
				if (!smr.sharedMesh) continue;
				CheckMesh(smr.sharedMesh, processed);
			}

			foreach (var mf in avatar.GetComponentsInChildren<MeshFilter>(true)) {
				if (!mf.sharedMesh) continue;
				CheckMesh(mf.sharedMesh, processed);
			}
		}

		private static void CheckMesh(Mesh mesh, HashSet<string> processed) {
			var path = AssetDatabase.GetAssetPath(mesh);
			if (string.IsNullOrEmpty(path)) return;
			if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return;

			var guid = AssetDatabase.AssetPathToGUID(path);
			if (!processed.Add(guid)) return;

			var meshName = mesh.name;

			if (!importer.isReadable)
				AvatarNotificationHelper.Set(new AvatarNotification(
					"mesh.rw." + guid,
					NotificationType.Warning,
					new[] { "avatar.editor.notification.mesh_rw", meshName },
					new AvatarAction[] {
						new(
							new[] { "avatar.editor.notification.mesh_rw.action.fix" },
							() => {
								importer.isReadable = true;
								importer.SaveAndReimport();
							}
						)
					}
				));

			var so       = new SerializedObject(importer);
			var legacyProp = so.FindProperty("m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes");
			if (legacyProp != null && !legacyProp.boolValue)
				AvatarNotificationHelper.Set(new AvatarNotification(
					"mesh.lbsn." + guid,
					NotificationType.Warning,
					new[] { "avatar.editor.notification.mesh_lbsn", meshName },
					new AvatarAction[] {
						new(
							new[] { "avatar.editor.notification.mesh_lbsn.action.fix" },
							() => {
								var s = new SerializedObject(importer);
								var p = s.FindProperty("m_LegacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes");
								if (p == null) return;
								p.boolValue = true;
								s.ApplyModifiedProperties();
								importer.SaveAndReimport();
							}
						)
					}
				));
		}
	}
}
