using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Build;
using Nox.CCK.Events;
using UnityEngine;
using Nox.CCK.Utils;

namespace Nox.CCK.Avatars {
	public sealed class AvatarDescriptor : MonoBehaviour, IAvatarDescriptor, ICompilable {
		public GameObject Anchor
			=> gameObject;

		#region Publisher

		#if UNITY_EDITOR
		/// <summary>
		/// Clés (<see cref="Platform.Key"/>) des plateformes ciblées, sérialisées : une par variant
		/// construit et publié.
		/// </summary>
		public string[] targetPlatforms = Array.Empty<string>();

		/// <summary>
		/// Plateformes que l'avatar publie : un variant (build + fichier) par entrée, dans l'ordre de
		/// <see cref="PlatformExtensions.All"/>. Vide = plateforme courante, voir <see cref="Compile"/>.
		/// </summary>
		public Platform[] Targets {
			get => (targetPlatforms ?? Array.Empty<string>())
				.Select(key => key.GetPlatformFromName())
				.Where(platform => platform != Platform.None)
				.Distinct()
				.OrderBy(platform => Array.IndexOf(PlatformExtensions.All, platform))
				.ToArray();
			set => targetPlatforms = (value ?? Array.Empty<Platform>())
				.Where(platform => platform != Platform.None)
				.Distinct()
				.OrderBy(platform => Array.IndexOf(PlatformExtensions.All, platform))
				.Select(platform => platform.Key)
				.ToArray();
		}

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
			if (Targets.Length == 0)
				Targets = new[] { PlatformExtensions.CurrentPlatform };
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

		#if UNITY_EDITOR
		/// <summary>
		/// Miroir sérialisé de <see cref="Modules"/> pour l'inspecteur : Unity ne sérialise pas un tableau
		/// typé par une interface, on expose donc les mêmes objets en <see cref="UnityEngine.Object"/>.
		/// Réécrit depuis la scène par <c>AvatarDescriptorEditor</c>, il ne sert qu'à l'affichage.
		/// </summary>
		public UnityEngine.Object[] detected = Array.Empty<UnityEngine.Object>();
		#endif

		public IAvatarModule[] Modules = Array.Empty<IAvatarModule>();

		/// <summary>
		/// Raised when a module joins the avatar.
		/// <para>
		/// The module set of an avatar changes at runtime: a module is added by a preparation step, a rig
		/// module is injected by another one, a module is destroyed on a hot-swap. Consumers that need to
		/// react (a controller binding itself to the rig, for instance) subscribe here instead of
		/// polling the module list every frame.
		/// </para>
		/// </summary>
		public NoxEvent<IAvatarModule> OnModuleRegistered { get; } = new();

		/// <summary>Raised when a module leaves the avatar (destroyed or removed).</summary>
		public NoxEvent<IAvatarModule> OnModuleUnregistered { get; } = new();

		public T[] GetModules<T>() where T : IAvatarModule
			=> Modules.OfType<T>().ToArray();

		IAvatarModule[] IAvatarDescriptor.Modules
			=> Modules;

		/// <summary>
		/// Refreshes the module list and raises the register/unregister events for the modules that
		/// joined or left the avatar.
		/// </summary>
		public IAvatarModule[] RefreshModules() {
			var previous = new HashSet<IAvatarModule>(Modules);
			var current  = FindModules(this);

			Modules = current;

			var registered = new HashSet<IAvatarModule>();
			var unregistered = new List<IAvatarModule>();

			for (var i = 0; i < current.Length; i++) {
				var module = current[i];
				if (module == null)
					continue;

				if (previous.Contains(module))
					continue;

				registered.Add(module);
			}

			foreach (var module in previous) {
				if (module == null)
					continue;

				if (registered.Contains(module))
					continue;

				if (current.Contains(module))
					continue;

				unregistered.Add(module);
			}

			foreach (var module in unregistered)
				OnModuleUnregistered.Invoke(module);

			foreach (var module in registered)
				OnModuleRegistered.Invoke(module);

			return current;
		}

		/// <summary>
		/// Modules de l'avatar : ceux du descriptor et de son sous-arbre. Contrairement aux worlds, la
		/// recherche reste limitée à la racine de l'avatar — une scène peut en contenir plusieurs.
		/// </summary>
		// ReSharper disable Unity.PerformanceAnalysis
		public static IAvatarModule[] FindModules(IAvatarDescriptor descriptor) {
			var modules = new HashSet<IAvatarModule>(descriptor.Modules);
			var root    = descriptor.Anchor;

			if (root)
				// Inclut le GameObject du descripteur lui-même
				modules.UnionWith(root.GetComponentsInChildren<IAvatarModule>(true));

			// The list seeds the next calls: a destroyed reference must not stay in it.
			modules.RemoveWhere(module => module is UnityEngine.Object element && element.IsDestroyed());

			#if UNITY_EDITOR
			// L'inspecteur affiche un tableau sérialisé (Unity ne sérialise pas IAvatarModule[]) : on le tient
			// à jour ici, donc à chaque détection. Trié par nom, l'ordre d'un HashSet n'étant pas déterministe.
			if (descriptor is AvatarDescriptor avatar) {
				var elements = modules.OfType<UnityEngine.Object>()
					.OrderBy(element => element.name, StringComparer.Ordinal)
					.ToArray();
				if (!(avatar.detected ?? Array.Empty<UnityEngine.Object>()).SequenceEqual(elements))
					avatar.detected = elements;
			}
			#endif

			return modules.ToArray();
		}

		#endregion Modules
	}
}