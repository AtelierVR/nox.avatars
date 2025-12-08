using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Avatars;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace Nox.Avatars.Runtime {
	public class AssetBundleRuntimeAvatar : BaseRuntimeAvatar {
		public AssetBundle Bundle;
		public string      Path;

		public bool CanUnloadAssetBundle() {
			foreach (var a0 in AvatarLoader.Avatar)
				if (a0 is AssetBundleRuntimeAvatar a1 && a1.Path == Path && a1.GetId() != GetId())
					return false;
			return true;
		}

		private static AssetBundle GetAssetBundle(string path) {
			foreach (var a0 in AvatarLoader.Avatar)
				if (a0 is AssetBundleRuntimeAvatar a1 && a1.Path == path)
					return a1.Bundle;
			return null;
		}


		public static async UniTask<AssetBundleRuntimeAvatar> Load(string path, Dictionary<string, object> arguments, Action<float> progress, CancellationToken token) {
			progress?.Invoke(0);

			var avatar = new AssetBundleRuntimeAvatar {
				Path   = path,
				Bundle = GetAssetBundle(path)
			};

			avatar.Bundle ??= await AssetBundle.LoadFromFileAsync(path)
				.ToUniTask(progress: new Progress<float>(p => progress?.Invoke(p * .25f)), cancellationToken: token);

			progress?.Invoke(.25f);

			if (!avatar.Bundle) {
				Logger.LogError($"Failed to load avatar from path: {path}");
				return null;
			}

			foreach (var asset in avatar.Bundle.GetAllAssetNames())
				Logger.LogDebug($"Bundle Asset: {asset}");


			// Load the avatar from the bundle (prefab)
			var obj = await avatar.Bundle.LoadAssetAsync<GameObject>("Avatar")
				.ToUniTask(
					progress: new Progress<float>(p => progress?.Invoke(.25f + p * .5f)),
					cancellationToken: token
				);

			var prefab = obj as GameObject;

			if (!prefab) {
				Logger.LogError($"No prefab found in avatar bundle: {path}");
				await avatar.Dispose();
				return null;
			}

			prefab.SetActive(false);

			avatar.Root = (await Object.InstantiateAsync(prefab)
					.ToUniTask(
						progress: new Progress<float>(p => progress?.Invoke(.75f + p * .25f)),
						cancellationToken: token
					)
				).FirstOrDefault();

			if (!avatar.Root) {
				Logger.LogError($"Failed to instantiate avatar prefab from bundle: {path}");
				await avatar.Dispose();
				return null;
			}


			avatar.Id         = avatar.Root.GetInstanceID().ToString();
			avatar.Root.name  = $"[{avatar.GetType().Name}_{avatar.GetId()}]";
			avatar.Descriptor = avatar.Root.GetComponent<IAvatarDescriptor>();

			if (avatar.Descriptor == null) {
				Logger.LogError($"Avatar prefab does not have a valid descriptor: {path}");
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

			progress?.Invoke(1);
			return avatar;
		}

		public override async UniTask Dispose() {
			if (Root) {
				Object.Destroy(Root);
				Root = null;
			}

			if (Bundle && CanUnloadAssetBundle()) {
				await Bundle.UnloadAsync(true);
				Bundle = null;
			}

			Descriptor = null;
		}
	}
}