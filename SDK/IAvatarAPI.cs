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

		public UniTask<IAvatar> Fetch(string identifier, string from = null);

		public ISearchRequest MakeSearchRequest();

		public IAssetSearchRequest MakeAssetSearchRequest();

		public UniTask<ISearchResponse> Search(ISearchRequest data, string from = null);

		public UniTask<IAvatar> Create(ICreateAvatarRequest data, string server);

		public UniTask<IAvatar> Update(string identifier, IUpdateAvatarRequest form, string from = null);

		public UniTask<bool> Delete(string identifier, string from = null);

		public UniTask<IAssetSearchResponse> SearchAssets(string identifier, IAssetSearchRequest data, string from = null);

		public UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, Action<float> onProgress = null);

		public UniTask<bool> UploadAssetFile(string identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, Action<float> onProgress = null);

		public UniTask<IAvatarAsset> CreateAsset(string identifier, ICreateAssetRequest data, string from = null);

		public ICaching DownloadToCache(string url, string hash = null, string from = null, UnityAction<float> progress = null, CancellationToken token = default);

		public void RemoveFromCache(string hash);

		public bool HasInCache(string hash);

		public UniTask<IAvatarIdentifier[]> AddFavorite(string identifier, string from = null);

		public UniTask<IAvatarIdentifier[]> RemoveFavorite(string identifier, string from = null);

		public UniTask<IAvatarIdentifier[]> GetFavorites(string from = null);
	}
}