using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using Nox.CCK.Utils;

namespace Nox.CCK.Avatars {
	public sealed class AvatarDescriptor : MonoBehaviour, IAvatarDescriptor, ICompilable {
		public GameObject Anchor
			=> gameObject;

		#region Publisher

		#if UNITY_EDITOR
		public Platform target;
		public uint     publishId;
		public string   publishServer;
		public ushort   publishVersion;
		#endif

		#endregion

		#region Build

		#if UNITY_EDITOR
		public bool isCompiled;

		public int CompileOrder
			=> 9999;

		// ReSharper disable Unity.PerformanceAnalysis
		public void Compile() {
			if (target == Platform.None)
				target = PlatformExtensions.CurrentPlatform;
			Modules    = FindModules(this);
			isCompiled = true;
		}
		#endif

		#endregion Build

		#region Animator

		private Animator _animator;

		// ReSharper disable Unity.PerformanceAnalysis
		public Animator Animator
			=> _animator ??= GetComponent<Animator>();

		#endregion Animator

		#region Modules

		[SerializeField]
		public IAvatarModule[] Modules = Array.Empty<IAvatarModule>();

		public T[] GetModules<T>() where T : IAvatarModule
			=> Modules.OfType<T>().ToArray();

		IAvatarModule[] IAvatarDescriptor.Modules
			=> Modules;

		// ReSharper disable Unity.PerformanceAnalysis
		public static IAvatarModule[] FindModules(IAvatarDescriptor descriptor) {
			var modules = new HashSet<IAvatarModule>(descriptor.Modules);
			var root    = descriptor.Anchor;
			modules.UnionWith(root.GetComponents<IAvatarModule>());
			modules.UnionWith(root.GetComponentsInChildren<IAvatarModule>(true));
			return modules.ToArray();
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public IAvatarModule[] RefreshModules()
			=> Modules = FindModules(this);

		#endregion Modules
	}
}