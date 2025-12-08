using System;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.Editor {
	// UI Management partial class - handles display states and UI updates
	public partial class PublisherInstance {
		private enum DisplayState {
			NotLogged,
			NoDescriptor,
			Loading,
			NotAttached,
			Attached
		}

		private DisplayState _currentState;

		private void UpdateDisplayState(DisplayState state = DisplayState.NotAttached) {
			_currentState = state;

			_notLoggedContainer?.SetDisplay(state == DisplayState.NotLogged);
			_noDescriptorContainer?.SetDisplay(state == DisplayState.NoDescriptor);
			_loadingContainer?.SetDisplay(state == DisplayState.Loading);
			_attachContainer?.SetDisplay(state == DisplayState.NotAttached);
			_attachedContainer?.SetDisplay(state == DisplayState.Attached);
			_assetContainer?.SetDisplay(state == DisplayState.Attached);
			_thumbnailContainer?.SetDisplay(state == DisplayState.Attached);

			_publishButton?.SetEnabled(state == DisplayState.Attached && AvatarNotificationHelper.Allowed);
		}

		private void UpdateAvatarUI() {
			if (_avatar == null) {
				_infoServerField?.SetValueWithoutNotify("");
				_infoIdField?.SetValueWithoutNotify(0);
				_infoNameField?.SetValueWithoutNotify("");
				_infoDescriptionField?.SetValueWithoutNotify("");
				UpdateThumbnailPreview();
				return;
			}

			_infoServerField?.SetValueWithoutNotify(_avatar.GetServerAddress() ?? "");
			_infoIdField?.SetValueWithoutNotify(_avatar.GetId());
			_infoNameField?.SetValueWithoutNotify(_avatar.GetTitle() ?? "");
			_infoDescriptionField?.SetValueWithoutNotify(_avatar.GetDescription() ?? "");
			UpdateThumbnailPreview();
		}

		private void UpdateThumbnailPreview() {
			if (_thumbnailField?.value is Texture2D localTexture) {
				UpdateThumbnailPreviewWithTexture(localTexture);
				return;
			}

			if (_avatar != null && !string.IsNullOrEmpty(_avatar.GetThumbnailUrl())) {
				DownloadAndDisplayThumbnail().Forget();
			} else {
				if (_thumbnailStatus != null)
					_thumbnailStatus.text = _avatar == null ? "No avatar attached" : "No thumbnail available";
				if (_thumbnailImage != null)
					_thumbnailImage.style.display = DisplayStyle.None;
				if (_thumbnailFixButton != null)
					_thumbnailFixButton.style.display = DisplayStyle.None;
			}
		}

		private async UniTask DownloadAndDisplayThumbnail() {
			if (_thumbnailPreview == null || _thumbnailStatus == null || _thumbnailImage == null || _avatar == null)
				return;

			try {
				_thumbnailStatus.text = "Loading thumbnail...";
				_thumbnailImage.style.display     = DisplayStyle.None;
				_thumbnailFixButton.style.display = DisplayStyle.None;

				var thumbnailUrl = _avatar.GetThumbnailUrl();
				var texture      = await Main.Instance.NetworkAPI.FetchTexture(thumbnailUrl);

				if (texture != null) {
					_thumbnailImage.image         = texture;
					_thumbnailImage.scaleMode     = ScaleMode.ScaleToFit;
					_thumbnailImage.style.display = DisplayStyle.Flex;
					_thumbnailStatus.text         = $"Current thumbnail - {texture.width}x{texture.height}";
				} else {
					_thumbnailStatus.text = "Failed to load thumbnail";
				}
			} catch (Exception ex) {
				Logger.LogError($"Failed to load thumbnail: {ex.Message}");
				_thumbnailStatus.text = "Failed to load thumbnail";
			}
		}

		private void UpdateThumbnailPreviewWithTexture(Texture2D texture) {
			if (_thumbnailPreview == null || _thumbnailStatus == null || _thumbnailImage == null)
				return;

			_thumbnailImage.style.display     = DisplayStyle.None;
			_thumbnailFixButton.style.display = DisplayStyle.None;

			if (texture == null) {
				_thumbnailStatus.text = "No thumbnail selected";
				return;
			}

			if (!texture.isReadable) {
				_thumbnailStatus.text             = "Texture must be readable";
				_thumbnailFixButton.text          = "Fix Automatically";
				_thumbnailFixButton.style.display = DisplayStyle.Flex;
				return;
			}

			var assetPath = AssetDatabase.GetAssetPath(texture);
			if (!string.IsNullOrEmpty(assetPath)) {
				var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
				if (importer != null && importer.textureType != TextureImporterType.Default) {
					_thumbnailStatus.text             = $"Texture type is '{importer.textureType}' (recommend 'Default')";
					_thumbnailFixButton.text          = "Fix Settings";
					_thumbnailFixButton.style.display = DisplayStyle.Flex;
					return;
				}
			}

			_thumbnailImage.image         = texture;
			_thumbnailImage.style.display = DisplayStyle.Flex;
			_thumbnailStatus.text         = $"Preview - {texture.width}x{texture.height} - Ready to upload";
		}

		private void ShowBuildProgress(float progress, string status) {
			if (_buildingContainer != null)
				_buildingContainer.style.display = DisplayStyle.Flex;
			if (_buildingProgressBar != null)
				_buildingProgressBar.value = progress * 100f;
			if (_buildingStatusLabel != null)
				_buildingStatusLabel.text = status;
			_publishButton?.SetEnabled(false);
		}

		private void HideBuildProgress() {
			if (_buildingContainer != null)
				_buildingContainer.style.display = DisplayStyle.None;
			_publishButton?.SetEnabled(_avatar != null && AvatarNotificationHelper.Allowed);
		}

		private void ShowResultDialog(bool success, string message) {
			if (_resultContainer == null) {
				if (success)
					EditorUtility.DisplayDialog("Success", message, "Ok");
				else
					EditorUtility.DisplayDialog("Error", message, "Ok");
				return;
			}

			_resultContainer.style.display    = DisplayStyle.Flex;
			_resultFailedLabel.style.display  = success ? DisplayStyle.None : DisplayStyle.Flex;
			_resultSuccessLabel.style.display = success ? DisplayStyle.Flex : DisplayStyle.None;
			_resultDetailsLabel.text          = message;
		}
	}

	// Extension helper for display management
	internal static class VisualElementExtensions {
		public static void SetDisplay(this VisualElement element, bool show) {
			if (element != null)
				element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}