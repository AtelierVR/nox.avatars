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
using Object = UnityEngine.Object;

namespace Nox.Avatars.Runtime
{
	public class AssetBundleRuntimeAvatar : BaseRuntimeAvatar
	{
		public IAsset Bundle;
		public string Path;
		public string CacheId;

		public static async UniTask<AssetBundleRuntimeAvatar> Load(string path, Dictionary<string, object> arguments, Action<float> progress, CancellationToken token)
		{
			progress?.Invoke(0);

			var avatar = new AssetBundleRuntimeAvatar
			{
				Path = path,
				CacheId = nameof(AssetBundleRuntimeAvatar) + "_" + Guid.NewGuid(),
				Arguments = arguments
			};

			try
			{
				try
				{
					avatar.Bundle = await GlobalAssetBundleManager.LoadFileAsync(
						path,
						avatar.CacheId,
						new Progress<float>(p => progress?.Invoke(p * .25f))
					);

					progress?.Invoke(.25f);
				}
				catch (Exception ex)
				{
					Logger.LogError(new Exception($"Exception while loading AssetBundle from path: {path}", ex));
					return null;
				}

				token.ThrowIfCancellationRequested();

				foreach (var asset in avatar.Bundle.AssetBundle.GetAllAssetNames())
					Logger.LogDebug($"Bundle Asset: {asset}");

				// Load the avatar from the bundle (prefab)

				var assetRequest = avatar.Bundle.AssetBundle.LoadAssetAsync<GameObject>("Avatar");

				// Yield périodique pendant le chargement pour ne pas bloquer
				while (!assetRequest.isDone)
				{
					token.ThrowIfCancellationRequested();
					progress?.Invoke(.25f + assetRequest.progress * .5f);
					await UniTask.Yield();
				}

				var obj = assetRequest.asset;

				var prefab = obj as GameObject;

				if (!prefab)
				{
					Logger.LogError($"No prefab found in avatar bundle: {path}");
					await avatar.Dispose();
					return null;
				}

				prefab.SetActive(false);

				avatar.Root = await prefab.InstantiateAsync(
					progress: new Progress<float>(p => progress?.Invoke(.75f + p * .25f)),
					cancellationToken: token
				);;

				if (!avatar.Root)
				{
					Logger.LogError($"Failed to instantiate avatar prefab from bundle: {path}");
					await avatar.Dispose();
					return null;
				}

				avatar.Id = avatar.Root.GetEntityId().GetHashCode().ToString();
				avatar.Root.name = $"[{avatar.GetType().Name}_{avatar.Id}]";
				avatar.Descriptor = avatar.Root.GetComponent<IAvatarDescriptor>();

				if (avatar.Descriptor == null)
				{
					Logger.LogError($"Avatar prefab does not have a valid descriptor: {path}");
					await avatar.Dispose();
					return null;
				}

				var result = await AvatarSetup.Prepare(
					avatar,
					progress: p => progress?.Invoke(.75f + p * .25f),
					token: token
				);

				if (!result)
				{
					Logger.LogError($"Failed to prepare avatar: {path}");
					await avatar.Dispose();
					return null;
				}

				progress?.Invoke(1);
				return avatar;
			}
			catch (OperationCanceledException)
			{
				await avatar.Dispose();
				return null;
			}
		}

		public override UniTask Dispose()
		{
			if (Root)
			{
				Root.Destroy();
				Root = null;
			}

			GlobalAssetBundleManager.DetachFile(Path, CacheId);
			Bundle = null;

			return UniTask.CompletedTask;
		}
	}
}