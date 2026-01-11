using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Avatars;
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
			if (avatar == null) return;
			_fetchEvent.Invoke(avatar);
			Main.Instance.CoreAPI.EventAPI.Emit("avatar_fetch", avatar);
		}

		public UniTask<Avatar> Fetch(IAvatarIdentifier identifier, string from = null, CancellationToken cancellationToken = default)
			=> Fetch(identifier.ToString(), from, cancellationToken);

		public UniTask<Avatar> Fetch(uint id, string from = null, CancellationToken cancellationToken = default)
			=> Fetch(id.ToString(), from, cancellationToken);

		public async UniTask<Avatar> Fetch(string identifier, string from = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}");
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
			if (Main.Instance.NetworkAPI == null)
				return null;

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search avatars: no server address provided.");
				return null;
			}

			var request = await RequestNode.To(address, $"/api/avatars{data.ToParams()}");
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
			if (Main.Instance.NetworkAPI == null)
				return null;

			if (string.IsNullOrEmpty(server)) {
				Logger.LogError("Cannot create avatar: no server address provided.");
				return null;
			}

			var request = await RequestNode.To(server, "/api/avatars");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar creation");
				return null;
			}

			request.SetBody(data);
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

		public async UniTask<Avatar> Update(AvatarIdentifier identifier, UpdateAvatarRequest form, string from = null, CancellationToken cancellationToken = default)
			=> await Update(identifier.ToString(), form, from, cancellationToken);

		public async UniTask<Avatar> Update(uint id, UpdateAvatarRequest form, string from = null, CancellationToken cancellationToken = default)
			=> await Update(id.ToString(), form, from, cancellationToken);

		public async UniTask<Avatar> Update(string identifier, UpdateAvatarRequest form, string from = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot update avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			request.SetBody(form);
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

		public async UniTask<bool> Delete(AvatarIdentifier identifier, string from = null, CancellationToken cancellationToken = default)
			=> await Delete(identifier.ToString(), from, cancellationToken);

		public async UniTask<bool> Delete(uint id, string from = null, CancellationToken cancellationToken = default)
			=> await Delete(id.ToString(), from, cancellationToken);

		public async UniTask<bool> Delete(string identifier, string from = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return false;

			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot delete avatar {identifier}: no server address provided.");
				return false;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}");
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

		public async UniTask<AssetSearchResponse> SearchAssets(AvatarIdentifier identifier, AssetSearchRequest data, string from = null, CancellationToken cancellationToken = default)
			=> await SearchAssets(identifier.ToString(), data, from, cancellationToken);

		public async UniTask<AssetSearchResponse> SearchAssets(uint id, AssetSearchRequest data, string from = null, CancellationToken cancellationToken = default)
			=> await SearchAssets(id.ToString(), data, from, cancellationToken);

		public async UniTask<AssetSearchResponse> SearchAssets(string identifier, AssetSearchRequest data, string from = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot get assets for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}/assets{data.ToParams()}");
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
			return response.Data;
		}

		public async UniTask<AvatarAsset> CreateAsset(AvatarIdentifier identifier, CreateAssetRequest data, string from = null, CancellationToken cancellationToken = default)
			=> await CreateAsset(identifier.ToString(), data, from, cancellationToken);

		public async UniTask<AvatarAsset> CreateAsset(uint id, CreateAssetRequest data, string from = null, CancellationToken cancellationToken = default)
			=> await CreateAsset(id.ToString(), data, from, cancellationToken);

		public async UniTask<AvatarAsset> CreateAsset(string identifier, CreateAssetRequest data, string from = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot create asset for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}/assets");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			request.SetBody(data);
			request.method = RequestExtension.Method.PUT;
			await request.Send(cancellationToken);
			var response = await request.Node<AvatarAsset>(cancellationToken);
			if (response.HasError()) {
				Logger.LogError($"Failed to create asset for avatar {identifier} on {address}: {response.Error.Message}");
				return null;
			}

			return response.Data;
		}

		public async UniTask<bool> UploadThumbnail(AvatarIdentifier identifier, Texture2D texture, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await UploadThumbnail(identifier.ToString(), texture, from, onProgress, cancellationToken);

		public async UniTask<bool> UploadThumbnail(uint id, Texture2D texture, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await UploadThumbnail(id.ToString(), texture, from, onProgress, cancellationToken);

		public async UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return false;

			if (texture == null) {
				Logger.LogError($"Cannot upload thumbnail for avatar {identifier}: texture is null.");
				return false;
			}

			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot upload thumbnail for avatar {identifier}: no server address provided.");
				return false;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			// Convert texture to PNG byte array
			byte[] imageData;
			string fileHash = null;

			try {
				imageData = texture.EncodeToPNG();

				if (imageData == null || imageData.Length == 0) {
					Logger.LogError($"Failed to encode texture for avatar {identifier}: EncodeToPNG returned null or empty data. Check texture format and read/write settings.");
					return false;
				}

				// Calculate hash for validation
				using (var sha256 = System.Security.Cryptography.SHA256.Create()) {
					var hashBytes = sha256.ComputeHash(imageData);
					fileHash = System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
				}
			} catch (System.Exception ex) {
				Logger.LogError($"Failed to encode texture for avatar {identifier}: {ex.Message}");
				return false;
			}

			// Create multipart form data manually
			var boundary = "----formdata-nox-" + Guid.NewGuid();
			var formData = $"--{boundary}\r\n";
			formData += "Content-Disposition: form-data; name=\"file\"; filename=\"thumbnail.png\"\r\n";
			formData += "Content-Type: image/png\r\n\r\n";

			// Combine header, image data, and footer
			var headerBytes = System.Text.Encoding.UTF8.GetBytes(formData);
			var footerBytes = System.Text.Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

			var bodyData = new byte[headerBytes.Length + imageData.Length + footerBytes.Length];
			System.Array.Copy(headerBytes, 0, bodyData, 0, headerBytes.Length);
			System.Array.Copy(imageData, 0, bodyData, headerBytes.Length, imageData.Length);
			System.Array.Copy(footerBytes, 0, bodyData, headerBytes.Length + imageData.Length, footerBytes.Length);

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}/thumbnail");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return false;
			}

			request.method = RequestExtension.Method.POST;
			request.SetBody(bodyData, $"multipart/form-data; boundary={boundary}");

			if (!string.IsNullOrEmpty(fileHash))
				request.SetRequestHeader("x-file-hash", fileHash);

			// Send request with progress monitoring if callback provided
			if (onProgress != null) {
				request.HandleUploadProgress((progress, _) => onProgress?.Invoke(progress), cancellationToken);
			}

			await request.Send(cancellationToken);

			if (!request.Ok()) {
				Logger.LogError($"Failed to upload thumbnail for avatar {identifier} on {address}");
				return false;
			}

			return true;
		}

		public async UniTask<UploadAssetResponse> UploadAssetFile(AvatarIdentifier identifier, uint assetId, byte[] fileData, string fileHash = null, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await UploadAssetFile(identifier.ToString(), assetId, fileData, fileHash, from, onProgress, cancellationToken);

		public async UniTask<UploadAssetResponse> UploadAssetFile(uint id, uint assetId, byte[] fileData, string fileHash = null, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await UploadAssetFile(id.ToString(), assetId, fileData, fileHash, from, onProgress, cancellationToken);

		public async UniTask<UploadAssetResponse> UploadAssetFile(string identifier, uint assetId, byte[] fileData, string fileHash = null, string from = null, System.Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot upload asset file for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			// Utiliser WWWForm pour une génération de multipart plus fiable
			var form = new WWWForm();
			form.AddBinaryData("file", fileData, "avatar.nox", "application/octet-stream");

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}/assets/{assetId}/file");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			request.method = RequestExtension.Method.POST;

			if (!string.IsNullOrEmpty(fileHash))
				request.SetRequestHeader("X-File-Hash", fileHash);

			request.HandleUploadProgress((f, b) => Logger.LogDebug($"Uploading asset file for avatar {identifier}: {f * 100f:0.00}% - {b} bytes"), cancellationToken);
			request.HandleDownloadProgress((f, b) => Logger.LogDebug($"Waiting for server response for avatar {identifier}: {f * 100f:0.00}% - {b} bytes"), cancellationToken);

			if (onProgress != null)
				request.HandleUploadProgress((progress, _) => onProgress?.Invoke(progress), cancellationToken);

			request.SetBody(form);
			
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

		public async UniTask<AssetStatusResponse> GetAssetStatus(AvatarIdentifier identifier, uint assetId, string from = null, CancellationToken cancellationToken = default)
			=> await GetAssetStatus(identifier.ToString(), assetId, from, cancellationToken);

		public async UniTask<AssetStatusResponse> GetAssetStatus(uint id, uint assetId, string from = null, CancellationToken cancellationToken = default)
			=> await GetAssetStatus(id.ToString(), assetId, from, cancellationToken);

		public async UniTask<AssetStatusResponse> GetAssetStatus(string identifier, uint assetId, string from = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot get asset status for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}/assets/{assetId}/status");
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

		public async UniTask<string> DownloadAssetFile(AvatarIdentifier identifier, uint assetId, string hash = null, string from = null, Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await DownloadAssetFile(identifier.ToString(), assetId, hash, from, onProgress, cancellationToken);

		public async UniTask<string> DownloadAssetFile(uint id, uint assetId, string hash = null, string from = null, Action<float> onProgress = null, CancellationToken cancellationToken = default)
			=> await DownloadAssetFile(id.ToString(), assetId, hash, from, onProgress, cancellationToken);

		public async UniTask<string> DownloadAssetFile(string identifier, uint assetId, string hash = null, string from = null, Action<float> onProgress = null, CancellationToken cancellationToken = default) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var output = Path.Join(Application.temporaryCachePath, string.IsNullOrEmpty(hash) ? $"{identifier}_{assetId}" : hash);
			var ide = AvatarIdentifier.From(identifier);

			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot download asset file for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			var request = await RequestNode.To(address, $"/api/avatars/{ide.ToString()}/assets/{assetId}/file");
			if (request == null) {
				Logger.LogError($"Failed to create request for avatar {identifier}");
				return null;
			}

			var downloadHandler = new DownloadHandlerFile(output) { removeFileOnAbort = true };
			request.downloadHandler = downloadHandler; // Use DownloadHandlerFile to save directly to file

			// Send request with progress monitoring if callback provided
			if (onProgress != null) {
				request.HandleDownloadProgress((progress, _) => onProgress?.Invoke(progress), cancellationToken);
			}

			await request.Send(cancellationToken);

			if (!request.Ok()) {
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

		public async UniTask<AvatarIdentifier[]> FetchFavorites(string from = null) {
			if (Main.Instance.TableAPI == null)
				return Array.Empty<AvatarIdentifier>();

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();

			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot fetch favorites: no server address provided.");
				return Array.Empty<AvatarIdentifier>();
			}

			var entry = await Main.Instance.TableAPI.Get("nox.avatars.favorites", address);
			if (entry != null)
				return entry
					.GetValue()
					.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => AvatarIdentifier.From(s.Trim()))
					.Where(i => i != null && i.IsValid())
					.Distinct()
					.ToArray();

			Logger.LogError($"Failed to fetch favorites from {address}: entry not found.");
			return Array.Empty<AvatarIdentifier>();
		}

		public async UniTask<AvatarIdentifier[]> AddFavorite(string identifier, string from = null)
			=> await AddFavorites(new[] { identifier }, from);

		public async UniTask<AvatarIdentifier[]> AddFavorites(string[] identifier, string from = null) {
			if (Main.Instance.TableAPI == null)
				return Array.Empty<AvatarIdentifier>();

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();

			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot add favorites: no server address provided.");
				return Array.Empty<AvatarIdentifier>();
			}

			var e = await FetchFavorites(from);

			var newE = identifier
				.Select(AvatarIdentifier.From)
				.Where(i => i != null && i.IsValid())
				.Concat(e)
				.Distinct()
				.ToArray();

			var entry = await Main.Instance.TableAPI.Set(
				"nox.avatars.favorites",
				string.Join(",", newE.Select(i => i.ToString())),
				address
			);

			if (entry != null)
				return newE;

			Logger.LogError($"Failed to add favorites on {address}: entry not found.");
			return e;
		}

		public async UniTask<AvatarIdentifier[]> RemoveFavorite(string identifier, string from = null)
			=> await RemoveFavorites(new[] { identifier }, from);

		public async UniTask<AvatarIdentifier[]> RemoveFavorites(string[] identifier, string from = null) {
			if (Main.Instance.TableAPI == null)
				return Array.Empty<AvatarIdentifier>();

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();

			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot remove favorites: no server address provided.");
				return Array.Empty<AvatarIdentifier>();
			}

			var e = await FetchFavorites(from);

			var newE = e
				.Where(i => !identifier.Contains(i.ToString()))
				.ToArray();

			var entry = await Main.Instance.TableAPI.Set(
				"nox.avatars.favorites",
				string.Join(",", newE.Select(i => i.ToString())),
				address
			);

			if (entry != null)
				return newE;

			Logger.LogError($"Failed to add favorites on {address}: entry not found.");
			return e;
		}
	}
}