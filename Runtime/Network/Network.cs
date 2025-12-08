using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Avatars;
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

		public UniTask<Avatar> Fetch(IAvatarIdentifier identifier, string from = null)
			=> Fetch(identifier.ToString(), from);

		public UniTask<Avatar> Fetch(uint id, string from = null)
			=> Fetch(id.ToString(), from);

		public async UniTask<Avatar> Fetch(string identifier, string from = null) {
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

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars/{ide.ToString()}");
			await request.Send();
			var response = request.GetMasterResponse<Avatar>();
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch avatar {identifier} from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var avatar = response.GetData();
			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<SearchResponse> Search(SearchRequest data, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search avatars: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars{data.ToParams()}");
			await request.Send();
			var response = request.GetMasterResponse<SearchResponse>();
			Logger.LogDebug(request.GetResponse<string>());
			if (response.HasError()) {
				Logger.LogError($"Failed to search avatars from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var avatars = response.GetData();

			foreach (var avatar in avatars.avatars)
				InvokeFetch(avatar);

			return avatars;
		}

		public async UniTask<Avatar> Create(CreateAvatarRequest data, string server) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			if (string.IsNullOrEmpty(server)) {
				Logger.LogError("Cannot create avatar: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(server, "/api/avatars");
			request.SetBody(data.ToJson(), "application/json");
			request.SetMethod("PUT");
			await request.Send();
			var response = request.GetMasterResponse<Avatar>();
			if (response.HasError()) {
				Logger.LogError($"Failed to create avatar on {server}: {response.GetError().GetMessage()}");
				return null;
			}

			var avatar = response.GetData();
			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<Avatar> Update(AvatarIdentifier identifier, UpdateAvatarRequest form, string from = null)
			=> await Update(identifier.ToString(), form, from);

		public async UniTask<Avatar> Update(uint id, UpdateAvatarRequest form, string from = null)
			=> await Update(id.ToString(), form, from);

		public async UniTask<Avatar> Update(string identifier, UpdateAvatarRequest form, string from = null) {
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

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars/{ide.ToString()}");
			request.SetBody(form.ToJson(), "application/json");
			request.SetMethod("POST");
			await request.Send();
			var response = request.GetMasterResponse<Avatar>();
			if (response.HasError()) {
				Logger.LogError($"Failed to update avatar {identifier} from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var avatar = response.GetData();
			InvokeFetch(avatar);
			return avatar;
		}

		public async UniTask<bool> Delete(AvatarIdentifier identifier, string from = null)
			=> await Delete(identifier.ToString(), from);

		public async UniTask<bool> Delete(uint id, string from = null)
			=> await Delete(id.ToString(), from);

		public async UniTask<bool> Delete(string identifier, string from = null) {
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

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars/{ide.ToString()}");
			request.SetMethod("DELETE");
			await request.Send();
			var response = request.GetMasterResponse<object>();
			if (!response.HasError()) return true;
			Logger.LogError($"Failed to delete avatar {identifier} from {address}: {response.GetError().GetMessage()}");
			return false;
		}

		public async UniTask<AssetSearchResponse> SearchAssets(AvatarIdentifier identifier, AssetSearchRequest data, string from = null)
			=> await SearchAssets(identifier.ToString(), data, from);

		public async UniTask<AssetSearchResponse> SearchAssets(uint id, AssetSearchRequest data, string from = null)
			=> await SearchAssets(id.ToString(), data, from);

		public async UniTask<AssetSearchResponse> SearchAssets(string identifier, AssetSearchRequest data, string from = null) {
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

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars/{ide.ToString()}/assets{data.ToParams()}");
			await request.Send();
			var response = request.GetMasterResponse<AssetSearchResponse>();
			if (!response.HasError()) return response.GetData();
			Logger.LogError($"Failed to get assets for avatar {identifier} from {address}: {response.GetError().GetMessage()}");
			return null;
		}

		public async UniTask<AvatarAsset> CreateAsset(AvatarIdentifier identifier, CreateAssetRequest data, string from = null)
			=> await CreateAsset(identifier.ToString(), data, from);

		public async UniTask<AvatarAsset> CreateAsset(uint id, CreateAssetRequest data, string from = null)
			=> await CreateAsset(id.ToString(), data, from);

		public async UniTask<AvatarAsset> CreateAsset(string identifier, CreateAssetRequest data, string from = null) {
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

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars/{ide.ToString()}/assets");
			request.SetBody(data.ToJson(), "application/json");
			request.SetMethod("PUT");
			await request.Send();
			var response = request.GetMasterResponse<AvatarAsset>();
			if (response.HasError()) {
				Logger.LogError($"Failed to create asset for avatar {identifier} on {address}: {response.GetError().GetMessage()}");
				return null;
			}

			return response.GetData();
		}

		public async UniTask<bool> UploadThumbnail(AvatarIdentifier identifier, Texture2D texture, string from = null, System.Action<float> onProgress = null)
			=> await UploadThumbnail(identifier.ToString(), texture, from, onProgress);

		public async UniTask<bool> UploadThumbnail(uint id, Texture2D texture, string from = null, System.Action<float> onProgress = null)
			=> await UploadThumbnail(id.ToString(), texture, from, onProgress);

		public async UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, System.Action<float> onProgress = null) {
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

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars/{ide.ToString()}/thumbnail");
			request.SetMethod("POST");
			request.SetBody(bodyData, $"multipart/form-data; boundary={boundary}");

			if (!string.IsNullOrEmpty(fileHash))
				request.SetHeader("x-file-hash", fileHash);

			// Send request with progress monitoring if callback provided
			if (onProgress != null) {
				var sendTask = request.Send();
				while (!sendTask.GetAwaiter().IsCompleted) {
					onProgress?.Invoke(request.GetUploadProgress());
					await UniTask.Yield();
				}

				await sendTask;           // Ensure the task completes
				onProgress?.Invoke(1.0f); // Ensure final progress is reported
			} else {
				await request.Send();
			}

			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to upload thumbnail for avatar {identifier} on {address}: {request.GetResponse<string>()}");
				return false;
			}

			return true;
		}

		public async UniTask<bool> UploadAssetFile(AvatarIdentifier identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, System.Action<float> onProgress = null)
			=> await UploadAssetFile(identifier.ToString(), assetId, fileData, fileName, fileHash, from, onProgress);

		public async UniTask<bool> UploadAssetFile(uint id, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, System.Action<float> onProgress = null)
			=> await UploadAssetFile(id.ToString(), assetId, fileData, fileName, fileHash, from, onProgress);

		public async UniTask<bool> UploadAssetFile(string identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, System.Action<float> onProgress = null) {
			if (Main.Instance.NetworkAPI == null)
				return false;
			var ide = AvatarIdentifier.From(identifier);
			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot upload asset file for avatar {identifier}: no server address provided.");
				return false;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			// Create multipart form data manually (same pattern as UploadThumbnail)
			var boundary = "----formdata-nox-" + Guid.NewGuid();
			var formData = $"--{boundary}\r\n";
			formData += $"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n";
			formData += "Content-Type: application/octet-stream\r\n\r\n";

			// Combine header, file data, and footer
			var headerBytes = System.Text.Encoding.UTF8.GetBytes(formData);
			var footerBytes = System.Text.Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

			var bodyData = new byte[headerBytes.Length + fileData.Length + footerBytes.Length];
			Array.Copy(headerBytes, 0, bodyData, 0, headerBytes.Length);
			Array.Copy(fileData, 0, bodyData, headerBytes.Length, fileData.Length);
			Array.Copy(footerBytes, 0, bodyData, headerBytes.Length + fileData.Length, footerBytes.Length);

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars/{ide.ToString()}/assets/{assetId}/file");
			request.SetMethod("POST");
			request.SetBody(bodyData, $"multipart/form-data; boundary={boundary}");
			// request.SetHeader("Content-Length", bodyData.Length.ToString());
			if (!string.IsNullOrEmpty(fileHash))
				request.SetHeader("x-file-hash", fileHash);

			// Send request with progress monitoring if callback provided
			if (onProgress != null) {
				onProgress.Invoke(0.0f); // Initialize progress to 0
				var sendTask = request.Send();
				while (!sendTask.GetAwaiter().IsCompleted) {
					onProgress.Invoke(request.GetUploadProgress());
					await UniTask.Yield();
				}

				await sendTask;          // Ensure the task completes
				onProgress.Invoke(1.0f); // Ensure final progress is reported
			} else await request.Send();

			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to upload asset file for avatar {identifier} on {address}: {request.GetResponse<string>()}");
				return false;
			}

			return true;
		}

		public async UniTask<string> DownloadAssetFile(AvatarIdentifier identifier, uint assetId, string hash = null, string from = null, Action<float> onProgress = null)
			=> await DownloadAssetFile(identifier.ToString(), assetId, hash, from, onProgress);

		public async UniTask<string> DownloadAssetFile(uint id, uint assetId, string hash = null, string from = null, Action<float> onProgress = null)
			=> await DownloadAssetFile(id.ToString(), assetId, hash, from, onProgress);

		public async UniTask<string> DownloadAssetFile(string identifier, uint assetId, string hash = null, string from = null, Action<float> onProgress = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var output = Path.Join(Application.temporaryCachePath, string.IsNullOrEmpty(hash) ? $"{identifier}_{assetId}" : hash);
			var ide    = AvatarIdentifier.From(identifier);

			if (ide.IsLocal())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), from);

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServer();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot download asset file for avatar {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServer())
				ide = new AvatarIdentifier(ide.GetId(), ide.GetMetadata(), AvatarIdentifier.LocalServer);

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/avatars/{ide.ToString()}/assets/{assetId}/file");
			var downloadHandler = new DownloadHandlerFile(output) { removeFileOnAbort = true };
			request.SetDownloadHandler(downloadHandler); // Use DownloadHandlerFile to save directly to file
			request.SetCacheDuration(0);                 // Disable caching because is not compatible with DownloadHandlerFile

			// Send request with progress monitoring if callback provided
			if (onProgress != null) {
				onProgress.Invoke(0.0f); // Initialize progress at 0
				var sendTask = request.Send();
				while (!sendTask.GetAwaiter().IsCompleted) {
					onProgress.Invoke(request.GetDownloadProgress());
					await UniTask.Yield();
				}

				await sendTask;          // Ensure the task completes
				onProgress.Invoke(1.0f); // Ensure final progress is reported
			} else {
				await request.Send();
			}

			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to download asset file for avatar {identifier} from {address}: {request.GetResponse<string>()}");
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