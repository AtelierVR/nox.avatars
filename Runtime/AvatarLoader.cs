using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace Nox.Avatars.Runtime {

    public class AvatarLoader {
        public static readonly List<IRuntimeAvatar> Avatar = new();

        public static readonly UnityEvent<IRuntimeAvatar> OnAdded = new();
        public static readonly UnityEvent<IRuntimeAvatar> OnRemoved = new();

        // Système de file d'attente pour limiter le nombre de chargements simultanés
        private static readonly SemaphoreSlim LoadingSemaphore = new SemaphoreSlim(3, 3);
        private static int _currentLoadingCount = 0;

        /// <summary>
        /// Nombre actuel d'avatars en cours de chargement
        /// </summary>
        public static int CurrentLoadingCount
            => _currentLoadingCount;

        /// <summary>
        /// Nombre maximum d'avatars pouvant être chargés simultanément
        /// </summary>
        public static int MaxConcurrentLoads
            => 3;

        internal static void InvokeAdded(IRuntimeAvatar runtimeAvatar) {
            Avatar.Add(runtimeAvatar);
            OnAdded.Invoke(runtimeAvatar);
            Main.Instance.CoreAPI.EventAPI.Emit("avatar_added", runtimeAvatar);
        }

        internal static void InvokeRemoved(IRuntimeAvatar runtimeAvatar) {
            Avatar.Remove(runtimeAvatar);
            OnRemoved.Invoke(runtimeAvatar);
            Main.Instance.CoreAPI.EventAPI.Emit("avatar_removed", runtimeAvatar);
        }

        [NoxPublic(NoxAccess.Method)]
        public static async UniTask<AssetBundleRuntimeAvatar> LoadFromCache(string hash, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default) {
            Logger.Log($"Loading avatar from cache: {hash}");

            var path = AvatarCache.GetIfExist(hash);
            if (!string.IsNullOrEmpty(path)) return await LoadFromPath(path, arguments, progress, token);

            Logger.LogError($"Avatar with hash {hash} not found in cache.");

            return null;
        }

        [NoxPublic(NoxAccess.Method)]
        public static async UniTask<AssetBundleRuntimeAvatar> LoadFromPath(string path, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default) {
            // Attendre qu'un slot de chargement soit disponible
            await LoadingSemaphore.WaitAsync(token);
            arguments ??= new Dictionary<string, object>();

            try {
                Interlocked.Increment(ref _currentLoadingCount);
                Logger.Log($"Loading avatar from path: {path} (Queue: {_currentLoadingCount}/{MaxConcurrentLoads})");

                var avatar = await AssetBundleRuntimeAvatar.Load(path, arguments, progress, token);
                if (avatar == null) {
                    Logger.LogError($"Failed to load avatar from path: {path}");
                    return null;
                }

                InvokeAdded(avatar);
                return avatar;
            } finally {
                Interlocked.Decrement(ref _currentLoadingCount);
                LoadingSemaphore.Release();
            }
        }

        [NoxPublic(NoxAccess.Method)]
        public static async UniTask<AssetRuntimeAvatar> LoadFromAssets(ResourceIdentifier path, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default) {
            // Attendre qu'un slot de chargement soit disponible
            await LoadingSemaphore.WaitAsync(token);
            arguments ??= new Dictionary<string, object>();

            try {
                Interlocked.Increment(ref _currentLoadingCount);
                Logger.Log($"Loading avatar from assets: {path} (Queue: {_currentLoadingCount}/{MaxConcurrentLoads})");

                var avatar = await AssetRuntimeAvatar.Load(path, arguments, progress, token);

                if (avatar == null) {
                    Logger.LogError($"Failed to load avatar from assets: {path}");
                    return null;
                }

                InvokeAdded(avatar);
                return avatar;
            } finally {
                Interlocked.Decrement(ref _currentLoadingCount);
                LoadingSemaphore.Release();
            }
        }
    }

}