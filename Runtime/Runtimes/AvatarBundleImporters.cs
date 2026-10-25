using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime {
	/// <summary>
	/// Extension point for avatar AssetBundles that are not Nox avatars (for example a VRChat
	/// <c>.vrca</c>): when <see cref="AssetBundleRuntimeAvatar"/> cannot find a Nox descriptor,
	/// <see cref="AvatarLoader"/> asks the registered importers to convert the bundle instead.
	/// </summary>
	public interface IAvatarBundleImporter {
		/// <summary>
		/// Loads the bundle's avatar. Return <c>null</c> when the bundle is not handled by this
		/// importer, so the next importer (or the standard Nox error) can take over.
		/// </summary>
		public UniTask<IRuntimeAvatar> Load(
			string                     path,
			Dictionary<string, object> arguments,
			Action<float>              progress,
			CancellationToken          token
		);
	}

	/// <summary>
	/// Registry of <see cref="IAvatarBundleImporter"/>s. Mods add their importer on initialization
	/// and remove it on disposal; the registry is empty when no compatibility mod is loaded.
	/// </summary>
	public static class AvatarBundleImporters {
		private static readonly List<IAvatarBundleImporter> Registered = new();

		public static IAvatarBundleImporter[] Importers
			=> Registered.ToArray();

		public static void Register(IAvatarBundleImporter importer) {
			if (importer == null || Registered.Contains(importer))
				return;

			Registered.Add(importer);
		}

		public static void Unregister(IAvatarBundleImporter importer)
			=> Registered.Remove(importer);

		/// <summary>
		/// Offers the bundle to every registered importer, in registration order.
		/// </summary>
		/// <returns>The first avatar an importer produced, or <c>null</c>.</returns>
		public static async UniTask<IRuntimeAvatar> TryLoad(
			string                     path,
			Dictionary<string, object> arguments,
			Action<float>              progress,
			CancellationToken          token
		) {
			foreach (var importer in Registered.ToArray()) {
				try {
					var avatar = await importer.Load(path, arguments, progress, token);
					if (avatar != null)
						return avatar;
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception exception) {
					Logger.LogWarning(
						$"Avatar bundle importer {importer.GetType().Name} failed for '{path}': {exception.Message}"
					);
				}
			}

			return null;
		}
	}
}
