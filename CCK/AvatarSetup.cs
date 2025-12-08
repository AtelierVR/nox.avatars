using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars {
	public static class AvatarSetup {
		private static readonly Type[] IncompatibleComponents = {
			typeof(AudioListener),
			typeof(Camera),
			typeof(FlareLayer),
			typeof(LightProbeGroup),
			typeof(ReflectionProbe),
			typeof(Terrain),
		};

		public static Func<IAvatarDescriptor, bool> OnCheckRequest;

		public static async UniTask<bool> Prepare(IRuntimeAvatar avatar, Action<float> progress = null, CancellationToken token = default) {
			if (avatar == null) {
				Logger.LogError("Avatar descriptor is null.");
				return false;
			}

			progress?.Invoke(0.0f);

			// Vérifier l'annulation dès le début
			if (token.IsCancellationRequested)
				return false;

			var descriptor = avatar.GetDescriptor();
			var gameObject = descriptor.GetAnchor();

			if (!gameObject) {
				Logger.LogError("Avatar descriptor root GameObject is null.");
				return false;
			}
			
			// Disable ApplyRootMotion on the Animator to avoid unwanted movements
			var animator = descriptor.GetAnimator();
			if (!animator) {
				Logger.LogError("Avatar descriptor Animator is null.");
				return false;
			}

			animator.applyRootMotion = false;

			// check is Humanoid and has Avatar
			if (animator.isHuman && animator.avatar) {
				// Ensure the avatar is valid
				if (!animator.avatar.isValid) {
					Logger.LogError("Animator avatar is not valid.");
					return false;
				}

				// Ensure the avatar is properly configured
				if (!animator.avatar.isHuman) {
					Logger.LogError("Animator avatar is not configured as Humanoid.");
					return false;
				}
			}

			descriptor.FindModules();

			if (OnCheckRequest == null)
				Logger.LogWarning("No OnCheckRequest is set, the avatar preparation will proceed without external validation.");
			var valid = OnCheckRequest?.Invoke(descriptor) ?? true;

			if (!valid) {
				Logger.LogError("A mod asked to cancel the avatar preparation.");
				return false;
			}

			descriptor.FindModules();

			if (token.IsCancellationRequested)
				return false;

			progress?.Invoke(0.1f);

			// Disable incompatible components
			foreach (var type in IncompatibleComponents) {
				var components = gameObject.GetComponentsInChildren(type, true);
				foreach (var comp in components) {
					switch (comp) {
						case Behaviour behaviour:
							behaviour.enabled = false;
							break;
						case Renderer renderer:
							renderer.enabled = false;
							break;
					}
				}
			}


			var compilable = gameObject
				.GetComponentsInChildren<ICompilable>(true)
				.OrderBy(c => c.CompileOrder)
				.ToArray();

			// Compilation des composants avec progression
			for (var i = 0; i < compilable.Length; i++) {
				if (token.IsCancellationRequested)
					return false;

				var c = compilable[i];
				if (c == null) {
					Logger.LogWarning($"Compilable component at index {i} is null, skipping.");
					continue;
				}

				Logger.LogDebug($"Compiling {c.GetType().Name} ({i + 1}/{compilable.Length})...");
				c.Compile();
				await c.CompileAsync();

				// Rapporter la progression (10% à 70% pour la compilation)
				var compileProgress = 0.2f + 0.5f * (i + 1) / compilable.Length;
				progress?.Invoke(compileProgress);
			}

			if (token.IsCancellationRequested)
				return false;
			progress?.Invoke(0.8f);

			var modules     = descriptor.GetModules();
			var moduleArray = modules.ToArray();

			// Initialisation des modules avec progression
			for (var i = 0; i < moduleArray.Length; i++) {
				if (token.IsCancellationRequested)
					return false;

				if (!await moduleArray[i].Setup(avatar)) {
					Logger.LogError($"Module {moduleArray[i].GetType().Name} failed to initialize.");
					return false;
				}

				// Rapporter la progression (80% à 100% pour les modules)
				var moduleProgress = 0.8f + 0.2f * (i + 1) / moduleArray.Length;
				progress?.Invoke(moduleProgress);
			}

			progress?.Invoke(1.0f);
			return true;
		}
	}
}