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
			var user       = Main.UserAPI.Current;
			var isLoggedIn = user != null && !string.IsNullOrEmpty(user.Server);

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
				_attachServerField.SetValueWithoutNotify(user.Server);

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

			if (!uint.TryParse(_attachIdField?.value ?? "", out var id))
				id = 0;

			var server = _attachServerField?.value;
			if (string.IsNullOrEmpty(server)) {
				var user = Main.UserAPI.Current;
				server = user?.Server;
			}

			if (string.IsNullOrEmpty(server)) {
				Logger.OpenDialog("Error", "No server address available.", "Ok");
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
			if (id > 0) {
				Logger.LogDebug($"Attempting to attach avatar {id}");
				avatar = await Main.Instance.Network.Fetch(id, server);
			}

			if (avatar == null && createIfNotFound) {
				Logger.LogDebug($"Avatar {id} not found, attempting to create new avatar.");
				avatar = await Main.Instance.Network.Create(new CreateAvatarRequest { Id = id }, server);
			}

			if (avatar != null) {
				var user          = Main.UserAPI.Current;
				var isContributor = user != null && user.Identifier.Equals(avatar.Owner);

				if (!isContributor) {
					Logger.OpenDialog("Error", "You are not a contributor of this avatar.", "Ok");
					Logger.LogError("You are not a contributor of this avatar.");
					UpdateDisplayState(DisplayState.NotAttached);
					return null;
				}
			}

			if (avatar == null) {
				if (createIfNotFound) {
					Logger.OpenDialog("Error", "Failed to create or find avatar.", "Ok");
					Logger.LogError("Failed to create or find avatar.");
				}

				UpdateDisplayState(DisplayState.NotAttached);
				return null;
			}

			descriptor.publishId     = avatar.Id;
			descriptor.publishServer = avatar.Server;
			EditorUtility.SetDirty(descriptor);
			_avatar = avatar;
			UpdateAvatarUI();
			UpdateDisplayState(DisplayState.Attached);
			return avatar;
		}

		private async UniTask OnRefreshInfoAsync() {
			if (_avatar == null)
				return;
			await AttachAvatarAsync(_avatar.Server, _avatar.Id, false);
		}

		private async UniTask OnUpdateInfoAsync() {
			if (_avatar == null) {
				Logger.OpenDialog("Error", "No avatar attached.", "Ok");
				return;
			}

			var name        = _infoNameField?.value ?? "";
			var description = _infoDescriptionField?.value ?? "";

			var success = await Main.Instance.Network.Update(
				_avatar.Id,
				new UpdateAvatarRequest {
					title       = name,
					description = description
				},
				_avatar.Server
			);

			if (success != null) {
				_avatar = success;
				UpdateAvatarUI();
			} else {
				Logger.OpenDialog("Error", "Failed to update avatar information.", "Ok");
			}
		}

		private async UniTask OnPublishAsync() {
			var descriptor = AvatarDescriptorHelper.CurrentAvatar;
			if (!descriptor) {
				Logger.OpenDialog("Error", "No descriptor found.", "Ok");
				return;
			}

			if (_avatar == null) {
				Logger.OpenDialog("Error", "No avatar attached. Please attach an avatar before publishing.", "Ok");
				return;
			}

			var target = descriptor.target;
			if (target == Platform.None)
				target = PlatformExtensions.CurrentPlatform;

			if (!target.IsSupported()) {
				Logger.OpenDialog("Error", $"{target.GetPlatformName()} is not supported.", "Ok");
				return;
			}

			var version = descriptor.publishVersion;
			if (version == 0) {
				Logger.OpenDialog("Error", "Asset version cannot be 0.", "Ok");
				return;
			}

			ShowBuildProgress(0f, "Verifying avatar...");
			_avatar = await Main.Instance.Network.Fetch(_avatar.Id, _avatar.Server);
			if (_avatar == null) {
				HideBuildProgress();
				Logger.OpenDialog("Error", "Failed to verify avatar.", "Ok");
				return;
			}

			var tempBuildPath = CreateTempBuildPath();
			var config        = Config.Load();
			try {
				// Check if asset already exists BEFORE building
				ShowBuildProgress(0.1f, "Checking existing assets...");

				var search = await Main.Instance.Network.SearchAssets(
					_avatar.Id,
					new AssetSearchRequest {
						Versions  = new[] { version },
						Platforms = new[] { target.GetPlatformName() },
						Engines   = new[] { Constants.CurrentEngine.GetEngineName() },
						ShowEmpty = true,
						Limit     = 1,
						Offset    = 0
					},
					_avatar.Server
				);

				var existingAsset         = search?.Items.FirstOrDefault();
				var assetAlreadyExists    = existingAsset is { IsEmpty: false };
				var strictVersionChecking = config.Get("sdk.strict_version", true);
				var autoVersion           = config.Get("sdk.auto_version", true);

				if (assetAlreadyExists) {
					// Auto-increment has priority: if enabled, increment instead of blocking or overwriting
					if (autoVersion) {
						// Auto-increment: use version+1 instead of overwriting
						version                   = (ushort)(version + 1);
						descriptor.publishVersion = version;
						EditorUtility.SetDirty(descriptor);
						if (_assetVersionField != null)
							_assetVersionField.value = version;

						Logger.Log($"Asset version {version - 1} already exists. Auto-incremented to version {version}");
					} else if (strictVersionChecking) {
						// Strict mode without auto-increment: block the upload
						HideBuildProgress();
						ShowResultDialog(false, $"Asset version {version} already exists for {target.GetPlatformName()}.\n\nPlease increment the version number, enable 'Auto increment version', or disable 'Strict version checking' to overwrite.");
						Logger.LogError($"Asset version {version} already exists. Strict version checking is enabled.");
						return;
					}
					// else: overwrite existing asset (strict is off, auto is off)
				}

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

				var filePath = Path.Combine(buildData.OutputPath, buildData.Filename);
				if (!File.Exists(filePath)) {
					HideBuildProgress();
					ShowResultDialog(false, "Built file not found: " + filePath);
					return;
				}

				ShowBuildProgress(0.75f, "Preparing file for upload...");
				var sizeMb = new FileInfo(filePath).Length / (1024f * 1024f);

				ShowBuildProgress(0.77f, $"Calculating file hash for {sizeMb:F1} MB file...");

				// Calculate file hash for validation
				var fileHash = Hashing.HashFile(filePath);

				Logger.Log($"File hash: {fileHash}");
				ShowBuildProgress(0.78f, $"Preparing asset entry...");

				// Search for asset again with the potentially updated version
				search = await Main.Instance.Network.SearchAssets(
					_avatar.Id,
					new AssetSearchRequest {
						Versions  = new[] { version },
						Platforms = new[] { target.GetPlatformName() },
						Engines   = new[] { Constants.CurrentEngine.GetEngineName() },
						ShowEmpty = true,
						Limit     = 1,
						Offset    = 0
					},
					_avatar.Server
				);

				var asset = search?.Items.FirstOrDefault();

				if (asset == null) {
					asset = await Main.Instance.Network.CreateAsset(
						_avatar.Id,
						new CreateAssetRequest {
							Version  = version,
							Engine   = Constants.CurrentEngine.GetEngineName(),
							Platform = target.GetPlatformName()
						},
						_avatar.Server
					);
				}

				if (asset == null) {
					HideBuildProgress();
					ShowResultDialog(false, "Failed to create or find asset entry.");
					return;
				}

				ShowBuildProgress(0.8f, $"Uploading {sizeMb:F1} MB file...");

				var uploadResponse = await Main.Instance.Network.UploadAssetFile(
					_avatar.Id,
					asset.Id,
					filePath,
					fileHash,
					_avatar.Server,
					onProgress: progress => {
						var sizeUploaded = progress * sizeMb;
						ShowBuildProgress(0.8f + progress * 0.1f, $"Uploading... {sizeUploaded:F2} MB / {sizeMb:F2} MB - {progress * 100:F0}%");
					}
				);

				if (uploadResponse == null) {
					HideBuildProgress();
					ShowResultDialog(false, "Failed to upload avatar file.");
					return;
				}

				Logger.Log($"Upload queued: {uploadResponse.Message} (Status: {uploadResponse.Status}, Queue position: {uploadResponse.QueuePosition})");

				// Poll asset status until processing is complete
				ShowBuildProgress(0.9f, $"Processing asset... (Queue position: {uploadResponse.QueuePosition})");

				const int maxAttempts  = 300; // 5 minutes max with 1 second interval
				var       attempt      = 0;
				var       isProcessing = true;
				var       nextTryAt    = uploadResponse.NextTryAt;

				while (isProcessing && attempt < maxAttempts) {
					// Calculate delay based on NextTryAt if available
					var delayMs = 1000; // Default 1 second
					if (nextTryAt > DateTime.UtcNow) {
						var timeUntilNextTry = (nextTryAt - DateTime.UtcNow).TotalMilliseconds;
						delayMs = (int)Math.Min(Math.Max(timeUntilNextTry, 100), 30000); // Between 100ms and 30s
						Logger.LogDebug($"Waiting {delayMs}ms until next status check (NextTryAt: {nextTryAt:u})");
					}

					await UniTask.Delay(delayMs);
					attempt++;

					var status = await Main.Instance.Network.GetAssetStatus(
						_avatar.Id,
						asset.Id,
						_avatar.Server
					);

					if (status == null) {
						Logger.LogWarning($"Failed to get asset status (attempt {attempt})");
						continue;
					}

					// Update nextTryAt from the status response
					if (status.NextTryAt > DateTime.UtcNow)
						nextTryAt = status.NextTryAt;

					Logger.LogDebug($"Asset status: {status.Status}, progress: {status.Progress}%, queue: {status.QueuePosition}");
					var processingProgress = 0.9f + (status.Progress / 100f) * 0.1f;

					switch (status.Status) {
						case AssetStatusType.PENDING:
							ShowBuildProgress(processingProgress, $"Waiting in queue... (Position: {status.QueuePosition})");
							break;
						case AssetStatusType.PROCESSING:
							ShowBuildProgress(processingProgress, $"Processing asset... {status.Progress}%");
							break;
						case AssetStatusType.COMPLETED:
							isProcessing = false;
							Logger.Log($"Asset processing completed. Hash: {status.Hash}, Size: {(status.Size >= 0 ? $"{status.Size} bytes" : "unknown")}");
							break;
						case AssetStatusType.FAILED:
							HideBuildProgress();
							ShowResultDialog(false, $"Asset processing failed: {status.Error ?? "Unknown error"}");
							return;
						default:
							Logger.LogWarning($"Unknown asset status: {status.Status}");
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
				Logger.LogError(new Exception("Failed to publish avatar", ex));
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
				Logger.OpenDialog("Error", "No descriptor selected.", "Ok");
				return;
			}

			if (_avatar == null) {
				Logger.OpenDialog("Error", "No avatar attached. Please attach an avatar first.", "Ok");
				return;
			}

			try {
				if (_assetDetectVersionButton != null)
					_assetDetectVersionButton.SetEnabled(false);

				Logger.Log("Detecting latest asset version...");

				// Search for all assets for this avatar
				var search = await Main.Instance.Network.SearchAssets(
					_avatar.Id,
					new AssetSearchRequest {
						ShowEmpty = true,
						Limit     = 1,
						Offset    = 0,
						Engines   = new[] { Constants.CurrentEngine.GetEngineName() },
						Versions  = new[] { ushort.MaxValue }
					},
					_avatar.Server
				);

				if (search == null) {
					Logger.OpenDialog("Error", "Failed to fetch asset versions from server.", "Ok");
					return;
				}

				ushort maxVersion = 0;

				var assets = search.Items;
				if (assets != null)
					foreach (var asset in assets) {
						var version = asset.Version;
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
				Logger.OpenDialog("Success", $"Version set to {nextVersion} (latest: {maxVersion})", "Ok");
			} catch (Exception ex) {
				Logger.OpenDialog("Error", $"Failed to detect version: {ex.Message}", "Ok");
				Logger.LogError($"Failed to detect version: {ex.Message}");
			} finally {
				_assetDetectVersionButton?.SetEnabled(true);
			}
		}
	}
}