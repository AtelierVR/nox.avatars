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

		private (string, string) Optimize(Identifier ide) {
			var crt = Main.UserAPI?.Current?.Server;
			if (!string.IsNullOrEmpty(crt))
				return ide.IsLocal(crt)
					? (ide.ToShortString(false), crt)
					: (ide.ToShortString(), crt);
			return (ide.ToShortString(), ide.Server);
		}

		public async UniTask<Avatar> Fetch(Identifier ide, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return null;
			}

			var request = await RequestNode.To(address, $"/avatars/{id}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide}");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<Avatar>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch avatar {ide} from {address}: {response.Error.Message}");
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
			Logger.LogDebug($"Body: {data.ToJson()}");
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

		public async UniTask<Avatar> Update(Identifier ide, UpdateAvatarRequest form, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return null;
			}

			var request = await RequestNode.To(address, $"/avatars/{id}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide}");
				return null;
			}

			request.SetBody(form.ToJson(), "application/json");
			request.method = RequestExtension.Method.POST;
			await request.Send(cancellationToken);
			var response = await request.Node<Avatar>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to update avatar {ide} from {address}: {response.Error.Message}");
				return null;
			}

			var avatar = response.Data;
			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<bool> Delete(Identifier ide, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return false;
			}

			var request = await RequestNode.To(address, $"/avatars/{id}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide}");
				return false;
			}

			request.method = RequestExtension.Method.DELETE;
			await request.Send(cancellationToken);
			if (request.Ok())
				return true;
			
			Logger.LogError($"Failed to delete avatar {ide} from {address}");
			return false;
		}

		public async UniTask<AssetSearchResponse> SearchAssets(Identifier ide, AssetSearchRequest data, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return null;
			}

			var request = await RequestNode.To(address, $"/avatars/{id}/assets{data.ToParams()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide} assets");
				return null;
			}

			await request.Send(cancellationToken);
			var response = await request.Node<AssetSearchResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to get assets for avatar {ide} from {address}: {response.Error.Message}");
				return null;
			}
			
			response.Data.Identifier = ide;
			response.Data.Request    = data;
			
			return response.Data;
		}

		public async UniTask<AvatarAsset> CreateAsset(Identifier ide, CreateAssetRequest data, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return null;
			}

			var request = await RequestNode.To(address, $"/avatars/{id}/assets");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide}");
				return null;
			}

			request.SetBody(data.ToJson(), "application/json");
			request.method = RequestExtension.Method.PUT;
			await request.Send(cancellationToken);
			var response = await request.Node<AvatarAsset>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to create asset for avatar {ide} on {address}: {response.Error.Message}");
				return null;
			}

			return response.Data;
		}

		public async UniTask<bool> UploadThumbnail(Identifier ide, Texture2D texture, Action<float> onProgress = null, CancellationToken cancellationToken = default){
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return false;
			}

			// Convert texture to PNG byte array
			byte[] imageData;
			string fileHash;

			try {
				imageData = texture.EncodeToPNG();

				if (imageData == null || imageData.Length == 0) {
					Logger.LogError($"Failed to encode texture for avatar {ide}: EncodeToPNG returned null or empty data. Check texture format and read/write settings.");
					return false;
				}

				fileHash = Hashing.HashBytes(imageData);
			} catch (Exception ex) {
				Logger.LogError($"Failed to encode texture for avatar {ide}: {ex.Message}");
				return false;
			}

			var request = await RequestNode.To(address, $"/avatars/{id}/thumbnail");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide}");
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
				Logger.LogError($"Failed during sending request to upload thumbnail for avatar {ide} on {address}");
				return false;
			}

			if (request.Ok())
				return true;
			
			Logger.LogError($"Failed to upload thumbnail for avatar {ide} on {address}");
			return false;

		}

		public async UniTask<UploadAssetResponse> UploadAssetFile(Identifier ide, uint assetId, string filePath, string fileHash = null, Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return null;
			}
			
			var request = await RequestNode.To(address, $"/avatars/{id}/assets/{assetId}/file");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide}");
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
				Logger.LogError($"Failed during sending request to upload asset file for avatar {ide} on {address}");
				return null;
			}

			if (request.responseCode != 202) {
				Logger.LogError($"Status code {request.responseCode} received when uploading asset file for avatar {ide} on {address}, expected 202 Accepted.");
				return null;
			}

			var response = await request.Node<UploadAssetResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to upload asset file for avatar {ide} on {address}: {response.Error.Message}");
				return null;
			}

			return response.Data;
		}

		public async UniTask<AssetStatusResponse> GetAssetStatus(Identifier ide, uint assetId, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return null;
			}

			var request = await RequestNode.To(address, $"/avatars/{id}/assets/{assetId}/status");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide}");
				return null;
			}

			await request.Send(cancellationToken);

			if (!request.Ok()) {
				Logger.LogError($"Failed to get asset status for avatar {ide} on {address}");
				return null;
			}

			var response = await request.Node<AssetStatusResponse>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to get asset status for avatar {ide} on {address}: {response.Error.Message}");
				return null;
			}

			return response.Data;
		}

		public async UniTask<string> DownloadAssetFile(Identifier ide, uint assetId, string hash = null, Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			var (id, address) = Optimize(ide);
			if (address == Identifier.LOCAL_SERVER) {
				Logger.LogError($"Cannot fetch world {ide} from {address}");
				return null;
			}
			
			var output = Path.Join(Application.temporaryCachePath, string.IsNullOrEmpty(hash) ? $"{ide}_{assetId}" : hash);

			var request = await RequestNode.To(address, $"/avatars/{id}/assets/{assetId}/file");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {ide}");
				return null;
			}

			// Use DownloadHandlerFile to save directly to file
			request.downloadHandler = new DownloadHandlerFile(output) { removeFileOnAbort = true };

			// Send request with progress monitoring if callback provided
			if (onProgress != null)
				request.HandleDownloadProgress((progress, _) => onProgress.Invoke(progress), cancellationToken);

			if (!await request.Send(cancellationToken) || !request.Ok()) {
				Logger.LogError($"Failed to download asset file for avatar {ide} from {address}");
				return null;
			}

			if (!File.Exists(output)) {
				Logger.LogError($"Downloaded asset file for avatar {ide} does not exist at expected path: {output}");
				return null;
			}

			if (!string.IsNullOrEmpty(hash) && Hashing.HashFile(output) != hash) {
				Logger.LogError($"Downloaded asset file for avatar {ide} does not match expected hash: {hash}");
				File.Delete(output); // Clean up if hash doesn't match
				return null;
			}

			Logger.LogDebug($"Successfully downloaded asset file for avatar {ide} to {output}");
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