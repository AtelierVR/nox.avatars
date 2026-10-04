using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Pipeline;
using Nox.Avatars.Editor;
using Nox.Avatars.Runtime.Network;
using Nox.CCK.Avatars;
using Nox.CCK.Convertors;
using Nox.CCK.Network.Assets;
using Nox.CCK.Utils;
using Nox.Network.Assets;
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
				avatar = await Main.Instance.Network.Fetch(new Identifier(AvatarIdentifierExtensions.AvatarType, id, null, server));
			}

			if (avatar == null && createIfNotFound) {
				Logger.LogDebug($"Avatar {id} not found, attempting to create new avatar.");
				avatar = await Main.Instance.Network.Create(new AvatarCreateRequest { Id = id }, server);
			}

			if (avatar != null) {
				var user          = Main.UserAPI.Current;
				var isContributor = user != null && avatar.IsContributor(user.Identifier);

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
				_avatar.Identifier,
				new AvatarUpdateRequest {
					Title       = name.ToTranslated(),
					Description = description.ToTranslated()
				}
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

			var targets = descriptor.Targets;
			if (targets.Length == 0)
				targets = new[] { PlatformExtensions.CurrentPlatform };

			foreach (var platform in targets)
				if (!platform.IsSupported()) {
					Logger.OpenDialog("Error", $"{platform.GetPlatformName()} is not supported.", "Ok");
					return;
				}

			var version = descriptor.publishVersion;
			if (version == 0) {
				Logger.OpenDialog("Error", "Asset version cannot be 0.", "Ok");
				return;
			}

			ShowBuildProgress(0f, "Verifying avatar...");
			_avatar = await Main.Instance.Network.Fetch(_avatar.Identifier);
			if (_avatar == null) {
				HideBuildProgress();
				Logger.OpenDialog("Error", "Failed to verify avatar.", "Ok");
				return;
			}

			var assets = Main.AssetsAPI;
			if (assets == null) {
				HideBuildProgress();
				Logger.OpenDialog("Error", "The asset pipeline is not available.", "Ok");
				return;
			}

			var tempBuildPath = CreateTempBuildPath();
			var config        = Config.Load();
			try {
				// A release is named after the version it publishes.
				ShowBuildProgress(0.1f, "Checking existing releases...");

				var server   = _avatar.Server;
				var assetRef = _avatar.Id.ToString();
				var engine   = Constants.CurrentEngine;
				var release  = await FetchRelease(assets, server, assetRef, version);

				var strictVersionChecking = config.Get("sdk.strict_version", true);
				var autoVersion           = config.Get("sdk.auto_version", true);

				if (release != null && autoVersion) {
					// Auto-increment has priority: publish under the next free version instead.
					var previous = version;

					while (release != null) {
						version++;
						release = await FetchRelease(assets, server, assetRef, version);
					}

					var liveVersion = AvatarDescriptorHelper.Live(descriptor);
					if (liveVersion) {
						liveVersion.publishVersion = version;
						EditorUtility.SetDirty(liveVersion);
					}
					if (_assetVersionField != null)
						_assetVersionField.value = version;

					Logger.Log($"Asset version {previous} already exists. Auto-incremented to version {version}");
				} else if (release != null && strictVersionChecking) {
					// Strict mode without auto-increment: block the upload
					HideBuildProgress();
					ShowResultDialog(false, $"Asset version {version} already exists.\n\nPlease increment the version number, enable 'Auto increment version', or disable 'Strict version checking' to overwrite.");
					Logger.LogError($"Asset version {version} already exists. Strict version checking is enabled.");
					return;
				}

				ShowBuildProgress(0.2f, $"Building avatar for {targets.Length} platform(s)...");

				var buildData = new BuildData {
					Descriptor       = descriptor,
					OutputPath       = tempBuildPath,
					ProgressCallback = (progress, status) => ShowBuildProgress(0.2f + (progress * 0.5f), status)
				};

				var result = await Builder.Build(buildData);
				if (result.Type != BuildResultType.Success) {
					HideBuildProgress();
					ShowResultDialog(false, $"Build failed: {result.Message}");
					return;
				}

				// Le build a pu recharger les scènes : on repart du descriptor vivant avant d'y écrire
				AvatarDescriptorHelper.Rebind();
				descriptor = AvatarDescriptorHelper.CurrentAvatar ?? descriptor;

				// Le build rapporte chaque variant avec sa plateforme : plus d'indexation par position
				var bundles = result.Outputs.ToDictionary(output => output.Platform, output => output.Path);

				if (bundles.Count != targets.Length) {
					HideBuildProgress();
					ShowResultDialog(false, $"Expected {targets.Length} bundle(s), got {bundles.Count}.");
					return;
				}

				foreach (var (platform, file) in bundles)
					if (!File.Exists(file)) {
						HideBuildProgress();
						ShowResultDialog(false, $"Built file not found for {platform.GetPlatformName()}: {file}");
						return;
					}

				ShowBuildProgress(0.75f, "Preparing release...");

				if (release == null)
					release = await assets.CreateRelease(
						server,
						Endpoint,
						assetRef,
						new AssetReleaseRequest {
							Name    = version.ToString(),
							Channel = AssetChannel.Stable
						}
					);

				if (release == null) {
					HideBuildProgress();
					ShowResultDialog(false, $"Failed to create release {version}.");
					return;
				}

				var published = new string[targets.Length];

				for (var i = 0; i < targets.Length; i++) {
					if (!bundles.TryGetValue(targets[i], out var bundlePath)) {
						HideBuildProgress();
						ShowResultDialog(false, $"No bundle was built for {targets[i].GetPlatformName()}.");
						return;
					}

					var error = await PublishVariant(assets, server, assetRef, release, targets[i], bundlePath, engine);
					if (error != null) {
						HideBuildProgress();
						ShowResultDialog(false, error);
						return;
					}

					published[i] = targets[i].GetPlatformName();
				}

				// La version est réappliquée sur le descriptor vivant : si le build a rechargé la scène
				// depuis le disque, la valeur en mémoire peut être celle d'avant le build.
				if (descriptor) {
					descriptor.publishVersion = version;
					EditorUtility.SetDirty(descriptor);
				}

				HideBuildProgress();
				ShowResultDialog(true, $"Avatar published successfully!\nVersion: {version}\nPlatforms: {string.Join(", ", published)}");
			} catch (Exception ex) {
				HideBuildProgress();
				ShowResultDialog(false, $"An error occurred: {ex.Message}");
				Logger.LogError(new Exception("Failed to publish avatar", ex));
			} finally {
				CleanupTempPath(tempBuildPath);
			}
		}

		/// <summary>
		/// Uploads <paramref name="filePath"/> as the <paramref name="platform"/> variant of the
		/// release, replacing the variant already published for that platform. Returns <c>null</c> on
		/// success, or the error to report.
		/// </summary>
		private async UniTask<string> PublishVariant(
			IAssetsAPI assets,
			string server,
			string assetRef,
			IAssetRelease release,
			Platform platform,
			string filePath,
			Engine engine
		) {
			var name   = platform.GetPlatformName();
			var length = new FileInfo(filePath).Length;
			var sizeMb = length / (1024f * 1024f);

			// Files are immutable: re-publishing the same version replaces the variant.
			var previous = release.BestFile(platform, engine);

			if (previous != null) {
				ShowBuildProgress(0.78f, $"Replacing the existing {name} variant...");

				if (string.IsNullOrEmpty(previous.Name)
					|| !await assets.DeleteFile(server, Endpoint, assetRef, release.Name, previous.Name))
					Logger.LogWarning($"Could not remove the previous '{previous.Name}' variant of version {release.Name}.");
			}

			ShowBuildProgress(0.79f, $"Hashing the {name} bundle ({sizeMb:F1} MB)...");

			var hash = await Hashing.HashFileAsync(
				AssetHash.Sha256,
				filePath,
				ratio => ShowBuildProgress(0.79f + (ratio * 0.05f), $"Hashing the {name} bundle... {ratio * 100:F0}%")
			);

			if (string.IsNullOrEmpty(hash))
				return $"Failed to hash the {name} bundle.";

			ShowBuildProgress(0.85f, $"Uploading {name} ({sizeMb:F1} MB)...");

			var uploaded = await assets.Upload(
				server,
				Endpoint,
				assetRef,
				release.Name,
				filePath,
				new AssetFileReservation {
					Name = Path.GetFileName(filePath),
					Mime = "application/octet-stream",
					Attributes = new[] {
						new AssetAttribute("platform", name),
						new AssetAttribute("engine", $"{engine.GetEngineName()}:{EngineVersion}")
					}
				},
				new AssetUploadOptions {
					Hash   = AssetHash.Parse(hash),
					Length = length
				},
				(sent, bytes) => {
					if (sent <= 0f && bytes > 0 && length > 0)
						sent = (float)((double)bytes / length);

					ShowBuildProgress(0.85f + (sent * 0.05f), $"Uploading {name}... {sent * sizeMb:F2} MB / {sizeMb:F2} MB - {sent * 100:F0}%");
				}
			);

			if (uploaded == null)
				return $"Failed to upload the {name} bundle.";

			ShowBuildProgress(0.9f, $"Processing the {name} bundle...");

			var processed = await WaitForProcessing(assets, server, assetRef, release.Name, uploaded, 0.9f);

			if (processed == null)
				return $"Processing the {name} bundle timed out. Please check the server status.";

			if (processed.Status?.Status == AssetState.Failed)
				return $"The {name} bundle was rejected: {processed.Status.Message ?? "Unknown error"}";

			return null;
		}

		/// <summary>The avatars collection served by the node.</summary>
		private static AssetEndpoint Endpoint
			=> AvatarsEndpoint.Endpoint;

		/// <summary>Release of <paramref name="version"/>, or <c>null</c> when it does not exist yet.</summary>
		private static async UniTask<IAssetRelease> FetchRelease(IAssetsAPI assets, string server, string asset, ushort version)
			=> await assets.FetchRelease(server, Endpoint, asset, version.ToString());

		/// <summary>Major and minor version of the running engine (<c>6000.4</c>), as stored in the
		/// <c>engine</c> file attribute.</summary>
		private static string EngineVersion {
			get {
				var version = EngineExtensions.CurrentVersion;
				return $"{version.Major}.{version.Minor}";
			}
		}

		/// <summary>
		/// Waits for the server to finish analyzing an uploaded file. The pipeline processes
		/// synchronously on small files, so this usually returns the file as-is; a file still
		/// pending is polled until it completes, fails, or the deadline is reached.
		/// </summary>
		private async UniTask<IAssetFile> WaitForProcessing(
			IAssetsAPI assets,
			string server,
			string asset,
			string release,
			IAssetFile file,
			float progress = 0.9f,
			float timeoutSeconds = 300f
		) {
			var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
			var current  = file;

			while (current?.Status is { Status: AssetState.Queued or AssetState.Processing }) {
				if (DateTime.UtcNow >= deadline) {
					Logger.LogError($"Asset processing timed out for {asset}/{release}/{current.Name}.");
					return null;
				}

				var delay = current.RefetchAt > DateTime.UtcNow
					? (current.RefetchAt - DateTime.UtcNow).TotalSeconds
					: 2d;

				ShowBuildProgress(progress, $"Processing asset... {current.Status.Progress}%");
				await UniTask.Delay(TimeSpan.FromSeconds(Math.Clamp(delay, 0.5d, 30d)));

				if (string.IsNullOrEmpty(current.Name))
					return current;

				current = await assets.FetchFile(server, Endpoint, asset, release, current.Name);

				if (current == null) {
					Logger.LogError($"Failed to read the status of {asset}/{release}/{file.Name}.");
					return null;
				}
			}

			Logger.Log($"Asset processing completed: {current?.Status?.Status} ({current?.Size ?? 0} bytes).");
			return current;
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

				ShowBuildProgress(0f, "Detecting latest version...");

				var assets = Main.AssetsAPI;
				var latest = 0;

				if (assets != null) {
					// The release the avatar points at is the newest one (`auto`), and it is named after
					// the version it publishes.
					var release = await assets.FetchPreferredRelease(_avatar.Server, Endpoint, _avatar.Id.ToString());

					if (release != null) {
						var name = release.Name?.TrimStart('v', 'V');

						if (!ushort.TryParse(name, out var detected))
							Logger.LogWarning($"Could not read a version out of release '{release.Name}'.");
						else
							latest = detected;
					}
				}

				HideBuildProgress();

				var nextVersion = (ushort)(latest + 1);
				descriptor.publishVersion = nextVersion;
				EditorUtility.SetDirty(descriptor);
				if (_assetVersionField != null)
					_assetVersionField.value = nextVersion;

				if (latest > 0)
					Logger.Log($"Detected latest version: {latest}. Set to {latest + 1}.");
				else
					Logger.Log("No existing version found. Set to 1.");
			} catch (Exception ex) {
				HideBuildProgress();
				Logger.OpenDialog("Error", $"Failed to detect version: {ex.Message}", "Ok");
				Logger.LogError($"Failed to detect version: {ex.Message}");
			} finally {
				_assetDetectVersionButton?.SetEnabled(true);
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
	}
}
