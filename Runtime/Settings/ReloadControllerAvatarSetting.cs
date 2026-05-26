using Cysharp.Threading.Tasks;
using Nox.Avatars.Controllers;
using Nox.CCK.Settings;
using Nox.Settings;
using Nox.UI;
using UnityEngine;

namespace Nox.Avatars.Runtime.Settings {
	public sealed class ReloadControllerAvatarSetting : ButtonHandler {
		public override string[] GetPath()
			=> new[] { "avatar", "general", "reload_avatar" };

		public override int GetOrder() => 1;

		public override void OnUpdated(IHandler handler)
			=> RefreshInteractable();

		public ReloadControllerAvatarSetting() {
			SetLabel("settings.entry.avatar.general.reload_avatar.label");
			SetButtonText("settings.entry.avatar.general.reload_avatar.action");
			RefreshInteractable();
		}

		protected override GameObject GetPrefab()
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/button.prefab");

		protected override void OnClick(IMenu menu)
			=> OnClickAsync().Forget();

		private async UniTask OnClickAsync() {
			var controller = Main.Instance?.ControllerAPI?.Current as IControllerAvatar;
			if (controller == null)
				return;

			SetInteractable(false);
			try {
				await controller.ReloadAvatar();
			} finally {
				RefreshInteractable();
			}
		}

		private void RefreshInteractable() {
			var controller = Main.Instance?.ControllerAPI?.Current as IControllerAvatar;
			SetInteractable(controller != null);
		}
	}
}