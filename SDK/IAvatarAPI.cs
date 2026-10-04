using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatar;
using Nox.CCK.Utils;
using Nox.Network.Assets;
using UnityEngine;
using UnityEngine.Events;

namespace Nox.Avatars {
	/// <summary>
	/// Interface for the Avatar API, providing methods to load avatars from various sources and to
	/// manage their metadata through the generic asset pipeline.
	/// </summary>
	public interface IAvatarAPI {
		#region Loading

		/// <summary>
		/// Creates a loading avatar.
		/// </summary>
		public UniTask<IRuntimeAvatar> LoadLoading(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Creates a default avatar.
		/// </summary>
		public UniTask<IRuntimeAvatar> LoadDefault(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Creates an error avatar.
		/// </summary>
		public UniTask<IRuntimeAvatar> LoadError(Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Load an avatar from a given path.
		/// </summary>
		public UniTask<IRuntimeAvatar> LoadFromPath(string path, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Load an avatar from a given mod assets.
		/// </summary>
		public UniTask<IRuntimeAvatar> LoadFromAssets(ResourceIdentifier path, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Load an avatar from a given cache hash.
		/// </summary>
		public UniTask<IRuntimeAvatar> LoadFromCache(string hash, Dictionary<string, object> arguments = null, Action<float> progress = null, CancellationToken token = default);

		#endregion

		#region Networking

		/// <summary>
		/// Fetches an avatar by its identifier.
		/// </summary>
		public UniTask<IAvatar> Fetch(Identifier identifier, CancellationToken token = default);

		/// <summary>
		/// Searches for avatars based on the provided search request.
		/// </summary>
		public UniTask<ISearchResponse> Search(ISearchRequest data);

		/// <summary>
		/// Creates a new avatar based on the provided creation request.
		/// </summary>
		public UniTask<IAvatar> Create(ICreateAvatarRequest data, string server);

		/// <summary>
		/// Updates an existing avatar with the provided update request.
		/// </summary>
		public UniTask<IAvatar> Update(Identifier identifier, IUpdateAvatarRequest form);

		/// <summary>
		/// Deletes an avatar by its identifier.
		/// </summary>
		public UniTask<bool> Delete(Identifier identifier);

		/// <summary>
		/// Resolves the bundle an avatar is loaded from: the file of its release that matches the
		/// current platform and engine, or <c>null</c> when the avatar has no compatible variant.
		/// </summary>
		public UniTask<IAssetFile> ResolveBundle(Identifier identifier, CancellationToken token = default);

		/// <summary>
		/// Adds an image to an avatar, converting the texture to PNG first.
		/// </summary>
		public UniTask<bool> AddImage(Identifier identifier, Texture2D texture, Action<float> onProgress = null);

		#endregion

		#region Caching

		public ICaching DownloadToCache(string url, string hash = null, UnityAction<float> progress = null, CancellationToken token = default);

		public ICaching GetDownload(string url, string hash);

		public void RemoveFromCache(string hash);

		public bool HasInCache(string hash);

		#endregion

		#region Favorites

		public UniTask<IFavorites> AddFavorite(Identifier identifier);

		public UniTask<IFavorites> RemoveFavorite(Identifier identifier);

		public UniTask<IFavorites> GetFavorites();

		#endregion
	}
}
