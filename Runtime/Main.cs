using System;
using System.Collections.Generic;
using System.Threading;
using Nox.Avatars.Runtime.Network;
using Cysharp.Threading.Tasks;
using Nox.Avatar;
using Nox.Avatars.Runtime.Caching;
using Nox.CCK.Avatars;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Controllers;
using Nox.Network;
using Nox.Search;
using Nox.Tables;
using Nox.Users;
using UnityEngine;
using UnityEngine.Events;
using Cache = UnityEngine.Cache;

namespace Nox.Avatars.Runtime
{

    public class Main : IMainModInitializer, IAvatarAPI
    {
        public static Main Instance;
        public IMainModCoreAPI CoreAPI;
        public Network.Network Network;
        internal CacheManager Cache;
        private Search.Search _search;
        private LanguagePack _lang;

        public INetworkAPI NetworkAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("network")
                       ?.GetInstance<INetworkAPI>();

        internal ISearchAPI SearchAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("search")
                       ?.GetInstance<ISearchAPI>();

        public IUserAPI UserAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("user")
                       ?.GetInstance<IUserAPI>();

        internal ITableAPI TableAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("table")
                       ?.GetInstance<ITableAPI>();

        internal IControllerAPI ControllerAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("controller")
                       ?.GetInstance<IControllerAPI>();

        public void OnInitializeMain(IMainModCoreAPI api)
        {
            Instance = this;
            CoreAPI = api;

            api.LoggerAPI.LogDebug("Initialized");
            _lang = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
            LanguageManager.AddPack(_lang);

            AvatarSetup.OnCheckRequest = OnCheckRequest;

            Network = new Network.Network();
            Cache = new CacheManager();
            _search = new Search.Search();
        }

        private bool OnCheckRequest(IAvatarDescriptor descriptor)
        {
            var valid = true;
            CoreAPI.EventAPI.Emit("avatar_check_request", descriptor, new Action<object[]>(OnCallback));
            return valid;

            void OnCallback(object[] args)
            {
                if (args.Length > 0 && args[0] is false) valid = false;
            }
        }

        public void OnDisposeMain()
        {
            AvatarSetup.OnCheckRequest = null;
            LanguageManager.RemovePack(_lang);
            Cache?.Dispose();
            Cache = null;
            _search?.Dispose();
            _search = null;
            Network = null;
            CoreAPI = null;
            Instance = null;
        }

        public async UniTask<IRuntimeAvatar> LoadLoading(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default)
        {
            var config = Config.Load();
            var custom = config.Get<string>(new[] { "avatar", "loading" });
            IRuntimeAvatar runtimeAvatar = null;
            if (!string.IsNullOrEmpty(custom)) runtimeAvatar = await AvatarLoader.LoadFromCache(custom, arguments, progress, token);
            runtimeAvatar ??= await AvatarLoader.LoadFromAssets("prefabs/loading.prefab", arguments, progress, token);
            runtimeAvatar ??= await LoadError(arguments, progress, token);
            return runtimeAvatar;
        }

        public async UniTask<IRuntimeAvatar> LoadDefault(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default)
        {
            var config = Config.Load();
            var custom = config.Get<string>(new[] { "avatar", "default" });
            IRuntimeAvatar runtimeAvatar = null;
            if (!string.IsNullOrEmpty(custom)) runtimeAvatar = await AvatarLoader.LoadFromCache(custom, arguments, progress, token);
            runtimeAvatar ??= await AvatarLoader.LoadFromAssets("prefabs/default.prefab", arguments, progress, token);
            runtimeAvatar ??= await LoadError(arguments, progress, token);
            return runtimeAvatar;
        }

        public async UniTask<IRuntimeAvatar> LoadError(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default)
        {
            var config = Config.Load();
            var custom = config.Get<string>(new[] { "avatar", "error" });
            IRuntimeAvatar runtimeAvatar = null;
            if (!string.IsNullOrEmpty(custom)) runtimeAvatar = await AvatarLoader.LoadFromCache(custom, arguments, progress, token);
            runtimeAvatar ??= await AvatarLoader.LoadFromAssets("prefabs/error.prefab", arguments, progress, token);
            return runtimeAvatar;
        }

        public async UniTask<IRuntimeAvatar> LoadFromPath(string path, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default)
            => await AvatarLoader.LoadFromPath(path, arguments, progress, token)
                ?? await LoadError(arguments, progress, token);

        public async UniTask<IRuntimeAvatar> LoadFromAssets(ResourceIdentifier path, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default)
            => await AvatarLoader.LoadFromAssets(path, arguments, progress, token)
                ?? await LoadError(arguments, progress, token);

        public async UniTask<IRuntimeAvatar> LoadFromCache(string hash, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default)
            => await AvatarLoader.LoadFromCache(hash, arguments, progress, token)
                ?? await LoadError(arguments, progress, token);

        public async UniTask<IAvatar> Fetch(string identifier, string from = null)
            => await Network.Fetch(identifier, from);

        public ISearchRequest MakeSearchRequest()
            => new SearchRequest();

        public IAssetSearchRequest MakeAssetSearchRequest()
            => new AssetSearchRequest();

        public async UniTask<IAvatar> Create(ICreateAvatarRequest data, string server)
            => await Network.Create(CreateAvatarRequest.FromBase(data), server);

        public async UniTask<IAvatar> Update(string identifier, IUpdateAvatarRequest form, string from = null)
            => await Network.Update(identifier, UpdateAvatarRequest.FromBase(form), from);

        public async UniTask<bool> Delete(string identifier, string from = null)
            => await Network.Delete(identifier, from);

        public async UniTask<IAssetSearchResponse> SearchAssets(string identifier, IAssetSearchRequest data, string from = null)
            => await Network.SearchAssets(identifier, AssetSearchRequest.FromBase(data), from);

        public async UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, Action<float> onProgress = null)
            => await Network.UploadThumbnail(identifier, texture, from, onProgress);

        public async UniTask<bool> UploadAssetFile(string identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, Action<float> onProgress = null)
            => await Network.UploadAssetFile(identifier, assetId, fileData, fileName, fileHash, from, onProgress);

        public async UniTask<IAvatarAsset> CreateAsset(string identifier, ICreateAssetRequest data, string from = null)
            => await Network.CreateAsset(identifier, CreateAssetRequest.FromBase(data), from);

        public ICaching DownloadToCache(string url, string hash = null, string from = null, UnityAction<float> progress = null, CancellationToken token = default)
        {
            var caching = Cache.AddDownload(url, hash, token);
            if (progress != null) caching.OnProgress.AddListener(progress);
            return caching;
        }

        public void RemoveFromCache(string hash)
            => Cache.Clear(hash);

        public bool HasInCache(string hash)
            => Cache.Has(hash);

        public async UniTask<IAvatarIdentifier[]> AddFavorite(string identifier, string from = null)
            => await Network.AddFavorite(identifier, from);

        public async UniTask<IAvatarIdentifier[]> RemoveFavorite(string identifier, string from = null)
            => await Network.RemoveFavorite(identifier, from);

        public async UniTask<IAvatarIdentifier[]> GetFavorites(string from = null)
            => await Network.FetchFavorites(from);

        public async UniTask<ISearchResponse> Search(ISearchRequest data, string from = null)
            => await Network.Search(SearchRequest.FromBase(data), from);
    }

}