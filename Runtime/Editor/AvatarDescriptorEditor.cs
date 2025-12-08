using Nox.CCK.Avatars;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nox.Avatars.Runtime.Editor {
	[CustomEditor(typeof(AvatarDescriptor))]
	public class AvatarDescriptorEditor : UnityEditor.Editor {
		private VisualElement _root;
		private PropertyField _modules;
		private PropertyField _target;
		private PropertyField _publishId;
		private PropertyField _publishServer;
		private PropertyField _publishVersion;

		private AvatarDescriptor module
			=> target as AvatarDescriptor;

		private void OnEnable()
			=> EditorApplication.hierarchyChanged += OnHierarchyChanged;

		private void OnDisable()
			=> EditorApplication.hierarchyChanged -= OnHierarchyChanged;


		private void OnHierarchyChanged() {
			if (Application.isPlaying || !module) return;
			module.Modules = module.FindModules();
			serializedObject.Update();
			if (_root != null) _root.Bind(serializedObject);
			serializedObject.ApplyModifiedProperties();
			Repaint();
		}

		public override VisualElement CreateInspectorGUI() {
			var visualTree = Resources.Load<VisualTreeAsset>("AvatarDescriptorEditor");
			_root           = visualTree.CloneTree();
			_modules        = _root.Q<PropertyField>("modules");
			_target         = _root.Q<PropertyField>("target");
			_publishId      = _root.Q<PropertyField>("publishId");
			_publishServer  = _root.Q<PropertyField>("publishServer");
			_publishVersion = _root.Q<PropertyField>("publishVersion");
			var targetProperty = serializedObject.FindProperty(nameof(AvatarDescriptor.target));
			if (targetProperty != null && _target != null)
				_target.BindProperty(targetProperty);
			var publishIdProperty = serializedObject.FindProperty(nameof(AvatarDescriptor.publishId));
			if (publishIdProperty != null && _publishId != null)
				_publishId.BindProperty(publishIdProperty);
			var publishServerProperty = serializedObject.FindProperty(nameof(AvatarDescriptor.publishServer));
			if (publishServerProperty != null && _publishServer != null)
				_publishServer.BindProperty(publishServerProperty);
			var publishVersionProperty = serializedObject.FindProperty(nameof(AvatarDescriptor.publishVersion));
			if (publishVersionProperty != null && _publishVersion != null)
				_publishVersion.BindProperty(publishVersionProperty);
			var modulesProperty = serializedObject.FindProperty(nameof(AvatarDescriptor.Modules));
			if (modulesProperty != null && _modules != null)
				_modules.BindProperty(modulesProperty);
			return _root;
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			if (_root != null)
				_root.Bind(serializedObject);
			serializedObject.ApplyModifiedProperties();
		}
	}
}