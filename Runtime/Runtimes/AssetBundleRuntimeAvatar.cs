using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.AssetBundles;
using Nox.CCK.AssetBundles;
using Nox.CCK.Avatars;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime {
	public class AssetBundleRuntimeAvatar : BaseRuntimeAvatar {
		public IAsset Bundle;
		public string Path;
		public string CacheId;

		public static async UniTask<AssetBundleRuntimeAvatar> Load(string path, Dictionary<string, object> arguments, Action<float> progress, CancellationToken token) {
			progress?.Invoke(0);

			var avatar = new AssetBundleRuntimeAvatar {
				Path = path,
				CacheId = nameof(AssetBundleRuntimeAvatar) + "_" + Guid.NewGuid(),
				Arguments = arguments
			};

			try {
				try {
					avatar.Bundle = await GlobalAssetBundleManager.LoadFileAsync(
						path,
						avatar.CacheId,
						new Progress<float>(p => progress?.Invoke(p * .25f))
					);

					progress?.Invoke(.25f);
				} catch (Exception ex) {
					Logger.LogError(new Exception($"Exception while loading AssetBundle from path: {path}", ex));
					return null;
				}

				token.ThrowIfCancellationRequested();
				try {
					var collections = avatar.Bundle.AssetBundle.LoadAllAssets<ShaderVariantCollection>();
					foreach (var collection in collections)
						if (collection && !collection.isWarmedUp)
							collection.WarmUp();
				} catch (Exception ex) {
					Logger.LogWarning(new Exception($"Failed to warmup shader variant collections", ex));
				}

				// Load the avatar from the bundle (prefab). A Nox bundle exposes it as "Avatar";
				// other builders (VRChat) use their own name, so fall back to the first prefab.
				var assetNames = avatar.Bundle.AssetBundle.GetAllAssetNames();
				var prefabName = assetNames.FirstOrDefault(name => name.Equals("Avatar", StringComparison.OrdinalIgnoreCase))
				              ?? assetNames.FirstOrDefault(name => name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase));

				if (string.IsNullOrEmpty(prefabName)) {
					Logger.LogDebug($"No prefab found in avatar bundle: {path}");
					await avatar.Dispose();
					return null;
				}

				var assetRequest = avatar.Bundle.AssetBundle.LoadAssetAsync<GameObject>(prefabName);

				// Yield périodique pendant le chargement pour ne pas bloquer
				while (!assetRequest.isDone) {
					token.ThrowIfCancellationRequested();
					progress?.Invoke(.25f + assetRequest.progress * .5f);
					await UniTask.Yield();
				}

				var obj = assetRequest.asset;

				var prefab = obj as GameObject;

				if (!prefab) {
					Logger.LogDebug($"No prefab found in avatar bundle: {path}");
					await avatar.Dispose();
					return null;
				}

				// Not a Nox avatar: let an importer (VRChat, …) handle the bundle without instantiating it.
				if (prefab.GetComponent<IAvatarDescriptor>() == null) {
					Logger.LogDebug($"Bundle '{path}' has no Nox AvatarDescriptor, trying the registered importers.");
					await avatar.Dispose();
					return null;
				}

				prefab.SetActive(false);

				avatar.Root = await prefab.InstantiateAsync(
					progress: new Progress<float>(p => progress?.Invoke(.75f + p * .25f)),
					cancellationToken: token
				);

				if (!avatar.Root) {
					Logger.LogError($"Failed to instantiate avatar prefab from bundle: {path}");
					await avatar.Dispose();
					return null;
				}

				avatar.Id = avatar.Root.GetId().ToString();
				avatar.Root.name = $"[{avatar.GetType().Name}_{avatar.Id}]";
				avatar.Descriptor = avatar.Root.GetComponent<IAvatarDescriptor>();

				if (avatar.Descriptor == null) {
					// Not a Nox avatar: let an importer (VRChat, …) handle the bundle.
					Logger.LogDebug($"Bundle '{path}' has no Nox AvatarDescriptor, trying the registered importers.");
					await avatar.Dispose();
					return null;
				}

				var result = await AvatarSetup.Prepare(
					avatar,
					progress: p => progress?.Invoke(.75f + p * .25f),
					token: token
				);

				if (!result) {
					Logger.LogError($"Failed to prepare avatar: {path}");
					await avatar.Dispose();
					return null;
				}

				// Route all non-voice AudioSources to the avatar channel's mixer track.
				if (avatar.Root.GetComponent<AvatarAudioGroup>() == null)
					avatar.Root.AddComponent<AvatarAudioGroup>();

				progress?.Invoke(1);
				return avatar;
			} catch (OperationCanceledException) {
				await avatar.Dispose();
				return null;
			} catch (Exception e) {
				await avatar.Dispose();
				Logger.LogError($"Unexpected error while loading avatar '{path}', the avatar has been disposed: {e}");
				return null;
			}
		}

		public override UniTask Dispose()
		{
			if (Root) {
				// Destroy immediately: the bundle is detached on the next line, and a deferred Destroy
				// would leave the instance alive for one frame with its assets already unloaded.
				Root.DestroyImmediate();
				Root = null;
			}

			GlobalAssetBundleManager.DetachFile(Path, CacheId);
			Bundle = null;

			return UniTask.CompletedTask;
		}
	}
}