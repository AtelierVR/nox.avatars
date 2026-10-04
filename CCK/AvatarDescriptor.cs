using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using UnityEngine.Serialization;
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

		public T[] GetModules<T>() where T : IAvatarModule
			=> Modules.OfType<T>().ToArray();

		IAvatarModule[] IAvatarDescriptor.Modules
			=> Modules;

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

			// La liste sert de graine aux appels suivants : une référence détruite ne doit pas y rester
			modules.RemoveWhere(module => module is UnityEngine.Object element && !element);

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

		// ReSharper disable Unity.PerformanceAnalysis
		public IAvatarModule[] RefreshModules()
			=> Modules = FindModules(this);

		#endregion Modules
	}
}