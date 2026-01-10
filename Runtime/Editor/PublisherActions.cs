using System;
using System.IO;
using System.Linq;
using Nox.Avatars.Runtime.Network;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Pipeline;
using Nox.Avatars.Editor;
using Nox.CCK.Utils;
using UnityEditor;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.Editor {
	// Actions partial class - handles attach, publish, and upload operations
	public partial class PublisherInstance {
		private async UniTask CheckLoginStatus() {
			var user       = Main.Instance.UserAPI.GetCurrent();
			var isLoggedIn = user != null && !string.IsNullOrEmpty(user.GetServerAddress());

			if (!isLoggedIn) {
				UpdateDisplayState(DisplayState.NotLogged);
				return;
			}

			var descriptor = AvatarDescriptorHelper.CurrentAvatar;
			if (!descriptor) {
				UpdateDisplayState(DisplayState.NoDescriptor);
				return;
			}

			if (_attachServerField != null)
				_attachServerField.SetValueWithoutNotify(user.GetServerAddress());

			if (descriptor.publishId > 0 && !string.IsNullOrEmpty(descriptor.publishServer)) {
				await AttachAvatarAsync(descriptor.publishServer, descriptor.publishId, false);
			} else {
				UpdateDisplayState(DisplayState.NotAttached);
			}
		}

		private async UniTask OnAttachAsync() {
			var descriptor = AvatarDescriptorHelper.CurrentAvatar;
			if (!descriptor) {
				UpdateDisplayState(DisplayState.NoDescriptor);
				return;
			}

			if (!uint.TryParse(_attachIdField?.value ?? "", out var id) || id == 0) {
				EditorUtility.DisplayDialog("Error", "Please enter a valid avatar ID.", "Ok");
				return;
			}

			var server = _attachServerField?.value;
			if (string.IsNullOrEmpty(server)) {
				var user = Main.Instance.UserAPI.GetCurrent();
				server = user?.GetServerAddress();
			}

			if (string.IsNullOrEmpty(server)) {
				EditorUtility.DisplayDialog("Error", "No server address available.", "Ok");
				return;
			}

			await AttachAvatarAsync(server, id, true);
		}

		private async UniTask<Network.Avatar> AttachAvatarAsync(string server, uint id, bool createIfNotFound) {
			var descriptor = AvatarDescriptorHelper.CurrentAvatar;
			if (!descriptor) {
				UpdateDisplayState(DisplayState.NoDescriptor);
				return null;
			}

			UpdateDisplayState(DisplayState.Loading);

			Network.Avatar avatar = null;
			if (id > 0)
				avatar = await Main.Instance.Network.Fetch(id, server);

			if (avatar == null && createIfNotFound)
				avatar = await Main.Instance.Network.Create(new CreateAvatarRequest { Id = id }, server);

			if (avatar != null) {
				var user          = Main.Instance.UserAPI.GetCurrent();
				var isContributor = user != null && user.ToIdentifier().Equals(avatar.GetOwnerId());

				if (!isContributor) {
					EditorUtility.DisplayDialog("Error", "You are not a contributor of this avatar.", "Ok");
					Logger.LogError("You are not a contributor of this avatar.");
					UpdateDisplayState(DisplayState.NotAttached);
					return null;
				}
			}

			if (avatar == null) {
				if (createIfNotFound) {
					EditorUtility.DisplayDialog("Error", "Failed to create or find avatar.", "Ok");
					Logger.LogError("Failed to create or find avatar.");
				}

				UpdateDisplayState(DisplayState.NotAttached);
				return null;
			}

			descriptor.publishId     = avatar.GetId();
			descriptor.publishServer = avatar.GetServerAddress();
			EditorUtility.SetDirty(descriptor);
			_avatar = avatar;
			UpdateAvatarUI();
			UpdateDisplayState(DisplayState.Attached);
			return avatar;
		}

		private async UniTask OnRefreshInfoAsync() {
			if (_avatar == null) return;
			await AttachAvatarAsync(_avatar.GetServerAddress(), _avatar.GetId(), false);
		}

		private async UniTask OnUpdateInfoAsync() {
			if (_avatar == null) {
				EditorUtility.DisplayDialog("Error", "No avatar attached.", "Ok");
				return;
			}

			var name        = _infoNameField?.value        ?? "";
			var description = _infoDescriptionField?.value ?? "";

			UpdateDisplayState(DisplayState.Loading);

			var success = await Main.Instance.Network.Update(
				_avatar.GetId(),
				new UpdateAvatarRequest {
					title       = name,
					description = description
				},
				_avatar.GetServerAddress()
			);

			if (success != null) {
				_avatar = success;
				UpdateAvatarUI();
				UpdateDisplayState(DisplayState.Attached);
				EditorUtility.DisplayDialog("Success", "Avatar information updated.", "Ok");
			} else {
				EditorUtility.DisplayDialog("Error", "Failed to update avatar information.", "Ok");
				UpdateDisplayState(DisplayState.Attached);
			}
		}

		private async UniTask OnPublishAsync() {
			var descriptor = AvatarDescriptorHelper.CurrentAvatar;
			if (!descriptor) {
				EditorUtility.DisplayDialog("Error", "No descriptor found.", "Ok");
				return;
			}

			if (_avatar == null) {
				EditorUtility.DisplayDialog("Error", "No avatar attached. Please attach an avatar before publishing.", "Ok");
				return;
			}

			var target = descriptor.target;
			if (target == Platform.None)
				target = PlatformExtensions.CurrentPlatform;

			if (!target.IsSupported()) {
				EditorUtility.DisplayDialog("Error", $"{target.GetPlatformName()} is not supported.", "Ok");
				return;
			}

			var version = descriptor.publishVersion;
			if (version == 0) {
				EditorUtility.DisplayDialog("Error", "Asset version cannot be 0.", "Ok");
				return;
			}

			ShowBuildProgress(0f, "Verifying avatar...");
			_avatar = await Main.Instance.Network.Fetch(_avatar.GetId(), _avatar.GetServerAddress());
			if (_avatar == null) {
				HideBuildProgress();
				EditorUtility.DisplayDialog("Error", "Failed to verify avatar.", "Ok");
				return;
			}

			var tempBuildPath = CreateTempBuildPath();
			try {
				ShowBuildProgress(0.2f, "Building avatar...");

				var buildData = new BuildData {
					Descriptor       = descriptor,
					Target           = target,
					OutputPath       = tempBuildPath,
					Filename         = descriptor.name + "_" + version + ".nox",
					ShowDialog       = false,
					ProgressCallback = (progress, status) => ShowBuildProgress(0.2f + (progress * 0.5f), status)
				};

				var result = await Builder.Build(buildData);
				if (result.Type != BuildResultType.Success) {
					HideBuildProgress();
					ShowResultDialog(false, $"Build failed: {result.Message}");
					return;
				}

				var builtFilePath = Path.Combine(buildData.OutputPath, buildData.Filename);
				if (!File.Exists(builtFilePath)) {
					HideBuildProgress();
					ShowResultDialog(false, "Built file not found: " + builtFilePath);
					return;
				}

				ShowBuildProgress(0.75f, "Preparing file for upload...");
				var fileData   = await File.ReadAllBytesAsync(builtFilePath);
				var fileSizeMB = fileData.Length / (1024.0 * 1024.0);

				ShowBuildProgress(0.77f, $"Calculating file hash for {fileSizeMB:F1} MB file...");

				// Calculate file hash for validation
				string fileHash = null;
				using (var sha256 = System.Security.Cryptography.SHA256.Create()) {
					var hashBytes = sha256.ComputeHash(fileData);
					fileHash = System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
				}

				Logger.Log($"File hash: {fileHash}");
				ShowBuildProgress(0.78f, $"Starting upload of {fileSizeMB:F1} MB file...");

				var search = await Main.Instance.Network.SearchAssets(
					_avatar.GetId(),
					new AssetSearchRequest {
						Versions  = new[] { version },
						Platforms = new[] { target.GetPlatformName() },
						Engines   = new[] { Constants.CurrentEngine.GetEngineName() },
						ShowEmpty = true,
						Limit     = 1,
						Offset    = 0
					},
					_avatar.GetServerAddress()
				);

				var asset = search?.GetAssets().FirstOrDefault();
				if (asset == null) {
					asset = await Main.Instance.Network.CreateAsset(
						_avatar.GetId(),
						new CreateAssetRequest {
							Version  = version,
							Engine   = Constants.CurrentEngine.GetEngineName(),
							Platform = target.GetPlatformName()
						},
						_avatar.GetServerAddress()
					);
				}

				if (asset == null) {
					HideBuildProgress();
					ShowResultDialog(false, "Failed to create or find asset entry.");
					return;
				}

				ShowBuildProgress(0.8f, $"Uploading {fileSizeMB:F1} MB file...");

				var uploadResponse = await Main.Instance.Network.UploadAssetFile(
					_avatar.GetId(),
					asset.GetId(),
					fileData,
					fileHash,
					_avatar.GetServerAddress(),
					onProgress: progress => {
						var sizeUploaded = progress * fileSizeMB;
						ShowBuildProgress(0.8f + progress * 0.1f, $"Uploading... {sizeUploaded:F2} MB / {fileSizeMB:F2} MB - {progress * 100:F0}%");
					}
				);

				if (uploadResponse == null || !uploadResponse.success) {
					HideBuildProgress();
					ShowResultDialog(false, uploadResponse?.message ?? "Failed to upload avatar file.");
					return;
				}

				Logger.Log($"Upload queued: {uploadResponse.message} (Status: {uploadResponse.status}, Queue position: {uploadResponse.queue_position})");
				
				// Poll asset status until processing is complete
				ShowBuildProgress(0.9f, $"Processing asset... (Queue position: {uploadResponse.queue_position})");
				
				var maxAttempts = 300; // 5 minutes max with 1 second interval
				var attempt = 0;
				var isProcessing = true;

				while (isProcessing && attempt < maxAttempts) {
					await UniTask.Delay(1000); // Wait 1 second between status checks
					attempt++;

					var status = await Main.Instance.Network.GetAssetStatus(
						_avatar.GetId(),
						asset.GetId(),
						_avatar.GetServerAddress()
					);

					if (status == null) {
						Logger.LogWarning($"Failed to get asset status (attempt {attempt})");
						continue;
					}

					Logger.LogDebug($"Asset status: {status.status}, progress: {status.progress}%, queue: {status.queue_position}");

					switch (status.status) {
						case "pending":
							ShowBuildProgress(0.9f, $"Waiting in queue... (Position: {status.queue_position})");
							break;
						case "processing":
							var processingProgress = 0.9f + (status.progress / 100f) * 0.1f;
							ShowBuildProgress(processingProgress, $"Processing asset... {status.progress}%");
							break;
						case "completed":
							isProcessing = false;
							Logger.Log($"Asset processing completed. Hash: {status.hash}, Size: {status.size} bytes");
							break;
						case "failed":
							HideBuildProgress();
							ShowResultDialog(false, $"Asset processing failed: {status.error ?? "Unknown error"}");
							return;
						case "empty":
							Logger.LogWarning("Asset status is empty, continuing...");
							break;
						default:
							Logger.LogWarning($"Unknown asset status: {status.status}");
							break;
					}
				}

				if (attempt >= maxAttempts) {
					HideBuildProgress();
					ShowResultDialog(false, "Asset processing timed out. Please check the server status.");
					return;
				}

				descriptor.publishVersion = version;
				EditorUtility.SetDirty(descriptor);

				HideBuildProgress();
				ShowResultDialog(true, $"Avatar published successfully!\nVersion: {version}\nPlatform: {target.GetPlatformName()}");
			} catch (Exception ex) {
				HideBuildProgress();
				ShowResultDialog(false, $"An error occurred: {ex.Message}");
				Logger.LogException(new Exception("Failed to publish avatar", ex));
			} finally {
				CleanupTempPath(tempBuildPath);
			}
		}

		private string CreateTempBuildPath() {
			var tempDir = Path.Combine(Path.GetTempPath(), "NoxAvatarBuild", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			return tempDir.Replace('\\', '/') + "/";
		}

		private void CleanupTempPath(string tempPath) {
			try {
				if (!string.IsNullOrEmpty(tempPath) && Directory.Exists(tempPath))
					Directory.Delete(tempPath, true);
			} catch (Exception ex) {
				Logger.LogError($"Failed to cleanup temporary directory: {ex.Message}");
			}
		}

		private async UniTask OnDetectVersionAsync() {
			var descriptor = AvatarDescriptorHelper.CurrentAvatar;
			if (!descriptor) {
				EditorUtility.DisplayDialog("Error", "No descriptor selected.", "Ok");
				return;
			}

			if (_avatar == null) {
				EditorUtility.DisplayDialog("Error", "No avatar attached. Please attach an avatar first.", "Ok");
				return;
			}

			try {
				if (_assetDetectVersionButton != null)
					_assetDetectVersionButton.SetEnabled(false);

				Logger.Log("Detecting latest asset version...");

				// Search for all assets for this avatar
				var search = await Main.Instance.Network.SearchAssets(
					_avatar.GetId(),
					new AssetSearchRequest {
						ShowEmpty = true,
						Limit     = 1,
						Offset    = 0,
						Engines   = new[] { Constants.CurrentEngine.GetEngineName() },
						Versions  = new[] { ushort.MaxValue }
					},
					_avatar.GetServerAddress()
				);

				if (search == null) {
					EditorUtility.DisplayDialog("Error", "Failed to fetch asset versions from server.", "Ok");
					return;
				}

				ushort maxVersion = 0;

				var assets = search.GetAssets();
				if (assets != null)
					foreach (var asset in assets) {
						var version = asset.GetVersion();
						if (version > maxVersion)
							maxVersion = version;
					}

				// Set the next version
				var nextVersion = (ushort)(maxVersion + 1);
				if (_assetVersionField != null)
					_assetVersionField.value = nextVersion;

				descriptor.publishVersion = nextVersion;
				EditorUtility.SetDirty(descriptor);

				Logger.Log($"Detected version: {maxVersion}, set to: {nextVersion}");
				EditorUtility.DisplayDialog("Success", $"Version set to {nextVersion} (latest: {maxVersion})", "Ok");
			} catch (Exception ex) {
				EditorUtility.DisplayDialog("Error", $"Failed to detect version: {ex.Message}", "Ok");
				Logger.LogError($"Failed to detect version: {ex.Message}");
			} finally {
				if (_assetDetectVersionButton != null)
					_assetDetectVersionButton.SetEnabled(true);
			}
		}
	}
}