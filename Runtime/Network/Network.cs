using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nox.CCK.Avatars;
using Nox.CCK.Convertors;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.Network {
	public class Network {
		private readonly UnityEvent<Avatar> _fetchEvent = new();

		private void InvokeFetch(Avatar avatar) {
			if (avatar == null)
				return;
			_fetchEvent.Invoke(avatar);
			Main.Instance.CoreAPI.EventAPI.Emit("avatar_fetch", avatar);
		}

		public UniTask<Avatar> Fetch(Identifier identifier, string from = null, CancellationToken cancellationToken = default)
			=> Fetch(identifier.ToString(), from, cancellationToken);

		public UniTask<Avatar> Fetch(uint id, string from = null, CancellationToken cancellationToken = default)
			=> Fetch(id.ToString(), from, cancellationToken);

		public async UniTask<Avatar> Fetch(string identifier, string from = null, CancellationToken cancellationToken = default) {
			var ide = Identifier.Parse(identifier);
			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);

			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<Avatar>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch avatar {identifier} from {address}: {response.Error.Message}");
				return null;
			}

			var avatar = response.Data;
			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<SearchResponse> Search(SearchRequest data, string from = null, CancellationToken cancellationToken = default) {
			var address = from ?? Main.UserAPI?.Current.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search avatars: no server address provided.");
				return null;
			}

			var request = await RequestNode.To(address, $"/avatars{data.ToParams()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar search");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<SearchResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to search avatars from {address}: {response.Error.Message}");
				return null;
			}

			var avatars = response.Data;

			foreach (var avatar in avatars.avatars)
				InvokeFetch(avatar);

			return avatars;
		}

		public async UniTask<Avatar> Create(CreateAvatarRequest data, string server, CancellationToken cancellationToken = default) {
			if (string.IsNullOrEmpty(server)) {
				Logger.LogError("Cannot create avatar: no server address provided.");
				return null;
			}

			var request = await RequestNode.To(server, "/avatars");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar creation");
				return null;
			}

			request.SetBody(data.ToJson(), "application/json");
			request.method = RequestExtension.Method.PUT;
			await request.Send(cancellationToken);
			var response = await request.Node<Avatar>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to create avatar on {server}: {response.Error.Message}");
				return null;
			}

			var avatar = response.Data;
			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<Avatar> Update(Identifier identifier, UpdateAvatarRequest form, string from = null, CancellationToken cancellationToken = default)
			=> await Update(identifier.ToString(), form, from, cancellationToken);

		public async UniTask<Avatar> Update(uint id, UpdateAvatarRequest form, string from = null, CancellationToken cancellationToken = default)
			=> await Update(id.ToString(), form, from, cancellationToken);

		public async UniTask<Avatar> Update(string identifier, UpdateAvatarRequest form, string from = null, CancellationToken cancellationToken = default) {
			var ide = Identifier.Parse(identifier);
			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);
			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot update avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			request.SetBody(form.ToJson(), "application/json");
			request.method = RequestExtension.Method.POST;
			await request.Send(cancellationToken);
			var response = await request.Node<Avatar>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to update avatar {identifier} from {address}: {response.Error.Message}");
				return null;
			}

			var avatar = response.Data;
			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<bool> Delete(Identifier identifier, string from = null, CancellationToken cancellationToken = default)
			=> await Delete(identifier.ToString(), from, cancellationToken);

		public async UniTask<bool> Delete(uint id, string from = null, CancellationToken cancellationToken = default)
			=> await Delete(id.ToString(), from, cancellationToken);

		public async UniTask<bool> Delete(string identifier, string from = null, CancellationToken cancellationToken = default) {
			var ide = Identifier.Parse(identifier);
			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);
			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot delete avatar {identifier}: no server address provided.");
				return false;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return false;
			}

			request.method = RequestExtension.Method.DELETE;
			await request.Send(cancellationToken);
			if (!request.Ok()) {
				Logger.LogError($"Failed to delete avatar {identifier} from {address}");
				return false;
			}
			return true;
		}

		public async UniTask<AssetSearchResponse> SearchAssets(Identifier identifier, AssetSearchRequest data, string from = null, CancellationToken cancellationToken = default)
			=> await SearchAssets(identifier.ToString(), data, from, cancellationToken);

		public async UniTask<AssetSearchResponse> SearchAssets(uint id, AssetSearchRequest data, string from = null, CancellationToken cancellationToken = default)
			=> await SearchAssets(id.ToString(), data, from, cancellationToken);

		public async UniTask<AssetSearchResponse> SearchAssets(string identifier, AssetSearchRequest data, string from = null, CancellationToken cancellationToken = default) {
			var ide = Identifier.Parse(identifier);
			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);
			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot get assets for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}/assets{data.ToParams()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier} assets");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<AssetSearchResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to get assets for avatar {identifier} from {address}: {response.Error.Message}");
				return null;
			}
			
			response.Data.Identifier = ide;
			response.Data.Request    = data;
			response.Data.Server     = address;
			
			return response.Data;
		}

		public async UniTask<AvatarAsset> CreateAsset(Identifier identifier, CreateAssetRequest data, string from = null, CancellationToken cancellationToken = default)
			=> await CreateAsset(identifier.ToString(), data, from, cancellationToken);

		public async UniTask<AvatarAsset> CreateAsset(uint id, CreateAssetRequest data, string from = null, CancellationToken cancellationToken = default)
			=> await CreateAsset(id.ToString(), data, from, cancellationToken);

		public async UniTask<AvatarAsset> CreateAsset(string identifier, CreateAssetRequest data, string from = null, CancellationToken cancellationToken = default) {
			var ide = Identifier.Parse(identifier);
			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);
			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot create asset for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}/assets");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			request.SetBody(data.ToJson(), "application/json");
			request.method = RequestExtension.Method.PUT;
			await request.Send(cancellationToken);
			var response = await request.Node<AvatarAsset>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to create asset for avatar {identifier} on {address}: {response.Error.Message}");
				return null;
			}

			return response.Data;
		}

		public async UniTask<bool> UploadThumbnail(Identifier identifier, Texture2D texture, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await UploadThumbnail(identifier.ToString(), texture, from, onProgress, cancellationToken);

		public async UniTask<bool> UploadThumbnail(uint id, Texture2D texture, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await UploadThumbnail(id.ToString(), texture, from, onProgress, cancellationToken);

		public async UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			if (!texture) {
				Logger.LogError($"Cannot upload thumbnail for avatar {identifier}: texture is null.");
				return false;
			}

			var ide = Identifier.Parse(identifier);
			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);
			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot upload asset file for avatar {identifier}: no server address provided.");
				return false;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			// Convert texture to PNG byte array
			byte[] imageData;
			string fileHash;

			try {
				imageData = texture.EncodeToPNG();

				if (imageData == null || imageData.Length == 0) {
					Logger.LogError($"Failed to encode texture for avatar {identifier}: EncodeToPNG returned null or empty data. Check texture format and read/write settings.");
					return false;
				}

				fileHash = Hashing.HashBytes(imageData);
			} catch (Exception ex) {
				Logger.LogError($"Failed to encode texture for avatar {identifier}: {ex.Message}");
				return false;
			}

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}/thumbnail");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return false;
			}

			request.method = RequestExtension.Method.POST;
			request.SetBody(new List<IMultipartFormSection>() {
				new MultipartFormFileSection(
					"file",
					imageData,
					"thumbnail.png",
					"image/png"
				)
			});

			if (!string.IsNullOrEmpty(fileHash))
				request.SetRequestHeader("x-file-hash", fileHash);

			// Send request with progress monitoring if callback provided
			if (onProgress != null)
				request.HandleUploadProgress((progress, _) => onProgress.Invoke(progress), cancellationToken);

			if (!await request.Send(cancellationToken)) {
				Logger.LogError($"Failed during sending request to upload thumbnail for avatar {identifier} on {address}");
				return false;
			}

			if (!request.Ok()) {
				Logger.LogError($"Failed to upload thumbnail for avatar {identifier} on {address}");
				return false;
			}

			return true;
		}

		public async UniTask<UploadAssetResponse> UploadAssetFile(Identifier identifier, uint assetId, string filePath, string fileHash = null, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await UploadAssetFile(identifier.ToString(), assetId, filePath, fileHash, from, onProgress, cancellationToken);

		public async UniTask<UploadAssetResponse> UploadAssetFile(uint id, uint assetId, string filePath, string fileHash = null, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await UploadAssetFile(id.ToString(), assetId, filePath, fileHash, from, onProgress, cancellationToken);

		public async UniTask<UploadAssetResponse> UploadAssetFile(string identifier, uint assetId, string filePath, string fileHash = null, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			var ide = Identifier.Parse(identifier);
			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);
			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot upload asset file for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}/assets/{assetId}/file");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			request.method = RequestExtension.Method.POST;
			if (onProgress != null)
				request.HandleUploadProgress((progress, _) => onProgress?.Invoke(progress), cancellationToken);

			request.SetBody(new List<IMultipartFormSection>() {
				new MultipartFormFileSection(
					"file",
					await File.ReadAllBytesAsync(filePath, cancellationToken),
					Path.GetFileName(filePath),
					"application/octet-stream"
				)
			});

			request.SetRequestHeader("Connection", "keep-alive");
			if (!string.IsNullOrEmpty(fileHash))
				request.SetRequestHeader("X-File-Hash", fileHash);

			if (!await request.Send(cancellationToken)) {
				Logger.LogError($"Failed during sending request to upload asset file for avatar {identifier} on {address}");
				return null;
			}

			if (request.responseCode != 202) {
				Logger.LogError($"Status code {request.responseCode} received when uploading asset file for avatar {identifier} on {address}, expected 202 Accepted.");
				return null;
			}

			var response = await request.Node<UploadAssetResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to upload asset file for avatar {identifier} on {address}: {response.Error.Message}");
				return null;
			}

			return response.Data;
		}

		public async UniTask<AssetStatusResponse> GetAssetStatus(Identifier identifier, uint assetId, string from = null, CancellationToken cancellationToken = default)
			=> await GetAssetStatus(identifier.ToString(), assetId, from, cancellationToken);

		public async UniTask<AssetStatusResponse> GetAssetStatus(uint id, uint assetId, string from = null, CancellationToken cancellationToken = default)
			=> await GetAssetStatus(id.ToString(), assetId, from, cancellationToken);

		public async UniTask<AssetStatusResponse> GetAssetStatus(string identifier, uint assetId, string from = null, CancellationToken cancellationToken = default) {
			var ide = Identifier.Parse(identifier);
			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);
			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot get asset status for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}/assets/{assetId}/status");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			await request.Send(cancellationToken);

			if (!request.Ok()) {
				Logger.LogError($"Failed to get asset status for avatar {identifier} on {address}");
				return null;
			}

			var response = await request.Node<AssetStatusResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to get asset status for avatar {identifier} on {address}: {response.Error.Message}");
				return null;
			}

			return response.Data;
		}

		public async UniTask<string> DownloadAssetFile(Identifier identifier, uint assetId, string hash = null, string from = null, Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await DownloadAssetFile(identifier.ToString(), assetId, hash, from, onProgress, cancellationToken);

		public async UniTask<string> DownloadAssetFile(uint id, uint assetId, string hash = null, string from = null, Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await DownloadAssetFile(id.ToString(), assetId, hash, from, onProgress, cancellationToken);

		public async UniTask<string> DownloadAssetFile(string identifier, uint assetId, string hash = null, string from = null, Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			var output = Path.Join(Application.temporaryCachePath, string.IsNullOrEmpty(hash) ? $"{identifier}_{assetId}" : hash);
			var ide    = Identifier.Parse(identifier);

			if (ide.IsLocal())
				ide = new Identifier(ide.Type, ide.Id, ide.Query, from);

			var address = from ?? Main.UserAPI?.Current.Server ?? ide.Server;
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot download asset file for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.Server)
				ide = new Identifier(ide.Type, ide.Id, ide.Query, Identifier.LOCAL_SERVER);

			var request = await RequestNode.To(address, $"/avatars/{ide.ToString()}/assets/{assetId}/file");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			// Use DownloadHandlerFile to save directly to file
			request.downloadHandler = new DownloadHandlerFile(output) { removeFileOnAbort = true };

			// Send request with progress monitoring if callback provided
			if (onProgress != null)
				request.HandleDownloadProgress((progress, _) => onProgress.Invoke(progress), cancellationToken);

			if (!await request.Send(cancellationToken) || !request.Ok()) {
				Logger.LogError($"Failed to download asset file for avatar {identifier} from {address}");
				return null;
			}

			if (!File.Exists(output)) {
				Logger.LogError($"Downloaded asset file for avatar {identifier} does not exist at expected path: {output}");
				return null;
			}

			if (!string.IsNullOrEmpty(hash) && Hashing.HashFile(output) != hash) {
				Logger.LogError($"Downloaded asset file for avatar {identifier} does not match expected hash: {hash}");
				File.Delete(output); // Clean up if hash doesn't match
				return null;
			}

			Logger.LogDebug($"Successfully downloaded asset file for avatar {identifier} to {output}");
			return output;
		}

		
		[Serializable]
		public class Favorites : IFavorites {
			[JsonProperty("label")]
			public string Label { get; set; }
			[JsonProperty("values"), JsonConverter(typeof(ArrayConverter<StringToIdentifierConverter>))]
			#pragma warning disable UAC1001
			public Identifier[] Values { get; set; }
			#pragma warning restore UAC1001
		}

		/// <summary>
		/// Fetch favorite avatars from the specified server
		/// </summary>
		/// <returns></returns>
		public async UniTask<Favorites> FetchFavorites(uint group = 0, bool pub = true) {
			var entry = await Main.Instance.TableAPI.Get($"{(pub ? "public." : "")}favorites.avatars.{group}");
			return entry != null
				? JsonConvert.DeserializeObject<Favorites>(entry.AsString)
				: null;
		}

		/// <summary>
		/// Add a avatar to favorites on the specified server
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="group"></param>
		/// <param name="pub"></param>
		/// <returns></returns>
		public async UniTask<Favorites> AddFavorite(Identifier identifier, uint group = 0, bool pub = true)
			=> await AddFavorites(new[] { identifier }, group, pub);

		/// <summary>
		/// Add avatars to favorites on the specified server
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="group"></param>
		/// <param name="pub"></param>
		/// <returns></returns>
		public async UniTask<Favorites> AddFavorites(Identifier[] identifier, uint group = 0, bool pub = true) {
			var e = await FetchFavorites();
			e.Values = identifier
				.Concat(e.Values)
				.Distinct()
				.ToArray();

			var entry = await Main.Instance.TableAPI.Set(
				$"{(pub ? "public." : "")}favorites.avatars.{group}",
				JsonConvert.SerializeObject(e)
			);

			if (entry != null)
				return null;

			Logger.LogError("Failed to add favorites: entry not found.");
			return e;
		}

		/// <summary>
		/// Remove a avatar from favorites on the specified server
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="group"></param>
		/// <param name="pub"></param>
		/// <returns></returns>
		public async UniTask<Favorites> RemoveFavorite(Identifier identifier, uint group = 0, bool pub = true)
			=> await RemoveFavorites(new[] { identifier }, group, pub);

		/// <summary>
		/// Remove avatars from favorites on the specified server
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="group"></param>
		/// <param name="pub"></param>
		/// <returns></returns>
		public async UniTask<Favorites> RemoveFavorites(Identifier[] identifier, uint group = 0, bool pub = true) {
			var e = await FetchFavorites();
			e.Values = e.Values
				.Where(i => !identifier.Contains(i))
				.ToArray();

			var entry = await Main.Instance.TableAPI.Set(
				$"{(pub ? "public." : "")}favorites.avatars.{group}",
				JsonConvert.SerializeObject(e)
			);

			if (entry != null)
				return null;

			Logger.LogError($"Failed to add favorites: entry not found.");
			return e;
		}
	}
}