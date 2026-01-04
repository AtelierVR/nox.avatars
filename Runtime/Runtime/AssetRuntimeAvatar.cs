using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Avatars;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace Nox.Avatars.Runtime {
	public class AssetRuntimeAvatar : BaseRuntimeAvatar {
		public ResourceIdentifier Path;

		public static async UniTask<AssetRuntimeAvatar> Load(ResourceIdentifier path, Dictionary<string, object> arguments, Action<float> progress, CancellationToken token) {
			progress?.Invoke(0);

			var avatar = new AssetRuntimeAvatar {
				Path      = path,
				Arguments = arguments
			};

			// Load the avatar from the bundle (prefab)
			var prefab = Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>(path);

			if (!prefab) {
				Logger.LogError($"No prefab found in avatar bundle: {path}");
				await avatar.Dispose();
				return null;
			}

			prefab.SetActive(false);

			progress?.Invoke(.1f);

			avatar.Root = (await Object.InstantiateAsync(prefab)
					.ToUniTask(progress: new Progress<float>(p => progress?.Invoke(.1f + p * .75f)), cancellationToken: token))
				.FirstOrDefault();

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
			await UniTask.Yield();

			if (Root) {
				Object.Destroy(Root);
				Root = null;
			}

			Descriptor = null;
		}
	}
}