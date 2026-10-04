using System;
using System.Linq;
using Nox.CCK.Avatars;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Nox.Avatars.Runtime.Editor {
	[CustomEditor(typeof(AvatarDescriptor))]
	public class AvatarDescriptorEditor : UnityEditor.Editor {
		private PropertyField _modules;
		private ListView      _modulesList;

		private AvatarDescriptor module
			=> target as AvatarDescriptor;

		private void OnEnable()
			=> EditorApplication.hierarchyChanged += RefreshModules;

		private void OnDisable()
			=> EditorApplication.hierarchyChanged -= RefreshModules;

		public override VisualElement CreateInspectorGUI() {
			var root = Resources.Load<VisualTreeAsset>("AvatarDescriptorEditor").CloneTree();

			BindField(root, "target", nameof(AvatarDescriptor.targetPlatforms));
			BindField(root, "publishId", nameof(AvatarDescriptor.publishId));
			BindField(root, "publishServer", nameof(AvatarDescriptor.publishServer));
			BindField(root, "publishVersion", nameof(AvatarDescriptor.publishVersion));

			_modules = root.Q<PropertyField>("modules");
			_modules.RegisterCallback<GeometryChangedEvent>(_ => LockModules());
			_modules.RegisterCallback<SerializedPropertyChangeEvent>(_ => _modules.schedule.Execute(() => RefreshModules()));

			BindField(root, "modules", nameof(AvatarDescriptor.detected));

			RefreshModules();
			return root;
		}

		private void BindField(VisualElement root, string elementName, string propertyName) {
			var field    = root.Q<PropertyField>(elementName);
			var property = serializedObject.FindProperty(propertyName);
			if (field != null && property != null)
				field.BindProperty(property);
		}

		// Liste déduite de l'avatar : les + / − et le réordonnancement n'ont pas de sens ici.
		private void LockModules() {
			var list = _modules?.Q<ListView>();
			if (list == null || list == _modulesList) return;

			_modulesList       = list;
			list.reorderable   = false;
			list.selectionType = SelectionType.None;
			list.allowAdd      = false;
			list.allowRemove   = false;
		}

		private void RefreshModules() {
			if (Application.isPlaying || _modules == null || !module) return;

			var found = AvatarDescriptor.FindModules(module);
			module.Modules = found;

			// Le tri garde une liste stable : FindModules renvoie l'ordre d'un HashSet.
			var modules = found.OfType<Object>().OrderBy(element => element.name, StringComparer.Ordinal).ToArray();
			if ((module.detected ?? Array.Empty<Object>()).SequenceEqual(modules)) return;

			var property = serializedObject.FindProperty(nameof(AvatarDescriptor.detected));
			property.arraySize = modules.Length;
			for (var i = 0; i < modules.Length; i++)
				property.GetArrayElementAtIndex(i).objectReferenceValue = modules[i];
			serializedObject.ApplyModifiedProperties();

			// La ListView interne du PropertyField se recale sur le tableau après un rebind.
			_modules.BindProperty(serializedObject.FindProperty(nameof(AvatarDescriptor.detected)));
		}
	}
}
