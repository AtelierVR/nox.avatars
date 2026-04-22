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

        public static INetworkAPI NetworkAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("network")
                       ?.GetInstance<INetworkAPI>();

        static internal ISearchAPI SearchAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("search")
                       ?.GetInstance<ISearchAPI>();

        public static IUserAPI UserAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("users")
                       ?.GetInstance<IUserAPI>();

        internal ITableAPI TableAPI
            => Instance.CoreAPI.ModAPI
                       .GetMod("tables")
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

        public async UniTask<IAvatar> Fetch(Identifier identifier)
            => await Network.Fetch(identifier);

        public ISearchRequest MakeSearchRequest()
            => new SearchRequest();

        public IAssetSearchRequest MakeAssetSearchRequest()
            => new AssetSearchRequest();

        public async UniTask<IAvatar> Create(ICreateAvatarRequest data, string server)
            => await Network.Create(CreateAvatarRequest.FromBase(data), server);

        public async UniTask<IAvatar> Update(Identifier identifier, IUpdateAvatarRequest form)
            => await Network.Update(identifier, UpdateAvatarRequest.FromBase(form));

        public async UniTask<bool> Delete(Identifier identifier)
            => await Network.Delete(identifier);

        public async UniTask<IAssetSearchResponse> SearchAssets(Identifier identifier, IAssetSearchRequest data)
            => await Network.SearchAssets(identifier, AssetSearchRequest.FromBase(data));

        public async UniTask<bool> UploadThumbnail(Identifier identifier, Texture2D texture, Action<float> onProgress = null)
            => await Network.UploadThumbnail(identifier, texture, onProgress);

        public async UniTask<IUploadAssetResponse> UploadAssetFile(Identifier identifier, uint assetId, string filePath, string fileHash = null, Action<float> onProgress = null)
            => await Network.UploadAssetFile(identifier, assetId, filePath, fileHash, onProgress);

        public async UniTask<IAssetStatusResponse> GetAssetStatus(Identifier identifier, uint assetId)
            => await Network.GetAssetStatus(identifier, assetId);

        public async UniTask<IAvatarAsset> CreateAsset(Identifier identifier, ICreateAssetRequest data)
            => await Network.CreateAsset(identifier, CreateAssetRequest.FromBase(data));

        public ICaching DownloadToCache(string url, string hash = null, UnityAction<float> progress = null, CancellationToken token = default)
        {
            var caching = Cache.AddDownload(url, hash, token);
            if (progress != null) caching.OnProgress.AddListener(progress);
            return caching;
        }

        public void RemoveFromCache(string hash)
            => Cache.Clear(hash);

        public bool HasInCache(string hash)
            => Cache.Has(hash);

        public async UniTask<IFavorites> AddFavorite(Identifier identifier)
            => await Network.AddFavorite(identifier);

        public async UniTask<IFavorites> RemoveFavorite(Identifier identifier)
            => await Network.RemoveFavorite(identifier);

        public async UniTask<IFavorites> GetFavorites()
            => await Network.FetchFavorites();

        public async UniTask<ISearchResponse> Search(ISearchRequest data, string from = null)
            => await Network.Search(SearchRequest.FromBase(data), from);
    }

}