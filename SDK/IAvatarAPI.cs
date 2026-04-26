using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatar;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.Avatars {
	public interface IAvatarAPI {
		/// <summary>
		/// Creates a loading avatar.
		/// </summary>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> LoadLoading(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Creates a default avatar.
		/// </summary>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> LoadDefault(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Creates an error avatar.
		/// </summary>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> LoadError(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Load an avatar from a given path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="arguments"></param>
		/// <param name="progress"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> LoadFromPath(string path, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Load an avatar from a given mod assets.
		/// </summary>
		/// <param name="modId"></param>
		/// <param name="path"></param>
		/// <param name="arguments"></param>
		/// <param name="progress"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> LoadFromAssets(ResourceIdentifier path, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Load an avatar from a given cache hash.
		/// </summary>
		/// <param name="hash"></param>
		/// <param name="arguments"></param>
		/// <param name="progress"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> LoadFromCache(string hash, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		public UniTask<IAvatar> Fetch(Identifier identifier);

		public UniTask<ISearchResponse> Search(ISearchRequest data);

		public UniTask<IAvatar> Create(ICreateAvatarRequest data, string server);

		public UniTask<IAvatar> Update(Identifier identifier, IUpdateAvatarRequest form);

		public UniTask<bool> Delete(Identifier identifier);

		public UniTask<IAssetSearchResponse> SearchAssets(Identifier identifier, IAssetSearchRequest data);

		public UniTask<bool> UploadThumbnail(Identifier identifier, Texture2D texture, Action<float> onProgress = null);

		public UniTask<IUploadAssetResponse> UploadAssetFile(Identifier identifier, uint assetId, string filePath, string fileHash = null, Action<float> onProgress = null);

		public UniTask<IAssetStatusResponse> GetAssetStatus(Identifier identifier, uint assetId);

		public UniTask<IAvatarAsset> CreateAsset(Identifier identifier, ICreateAssetRequest data);

		public ICaching DownloadToCache(string url, string hash = null, UnityAction<float> progress = null, CancellationToken token = default);

		public void RemoveFromCache(string hash);

		public bool HasInCache(string hash);

		public UniTask<IFavorites> AddFavorite(Identifier identifier);

		public UniTask<IFavorites> RemoveFavorite(Identifier identifier);

		public UniTask<IFavorites> GetFavorites();
	}
}