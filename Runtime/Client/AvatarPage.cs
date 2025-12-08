using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Avatars;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.Users;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.client {
	public class AvatarPage : IPage {
		internal static string GetStaticKey()
			=> "avatar";

		public string GetKey()
			=> GetStaticKey();

		internal int               MId;
		private  object[]          _context;
		private  GameObject        _content;
		private  AvatarComponent   _component;
		private  IAvatarIdentifier _identifier;
		public   IAvatar           Avatar;
		public   IAvatarAsset      Asset;
		public   ushort            Version = ushort.MaxValue;
		private  bool              _isLoading;

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnRefresh()
			=> Refresh(false).Forget();

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}


		internal static IPage OnGotoAction(IMenu menu, object[] context) {
			if (!T(context, 0, out string type)) return null;
			switch (type) {
				case "id_server" when T(context, 1, out uint id0) && T(context, 2, out string ser0):
					return OnPageByIdentifier(menu, context, new AvatarIdentifier(id0, null, ser0));
				case "identifier" when T(context, 1, out string id2):
					return OnPageByIdentifier(menu, context, AvatarIdentifier.From(id2));
				case "identifier" when T(context, 1, out IAvatarIdentifier ai0):
					return OnPageByIdentifier(menu, context, AvatarIdentifier.From(ai0));
				case "avatar" when T(context, 1, out IAvatar a0):
					var asset0 = T(context, 2, out IAvatarAsset avatarAsset) ? avatarAsset : null;
					return OnPageByAvatar(menu, context, a0, asset0);
			}

			return null;
		}

		private static AvatarPage OnPageByIdentifier(IMenu menu, object[] context, AvatarIdentifier identifier) {
			var page = new AvatarPage {
				MId         = menu.GetId(),
				_context    = context,
				_identifier = identifier,
				Avatar      = null,
				Asset       = null,
				Version     = identifier.GetVersion()
			};
			page.Refresh(true).Forget();
			return page;
		}

		private static AvatarPage OnPageByAvatar(IMenu menu, object[] context, IAvatar avatar, IAvatarAsset asset) {
			var page = new AvatarPage {
				MId         = menu.GetId(),
				_context    = context,
				_identifier = avatar.GetIdentifier(),
				Avatar      = avatar,
				Asset       = asset,
				Version     = avatar.GetIdentifier().GetVersion()
			};
			if (page.Asset == null)
				page.FetchAsset(false).Forget();
			return page;
		}

		private async UniTask Refresh(bool load) {
			if (_isLoading) return;
			await FetchAvatar();
			await FetchAsset();
			_component.UpdateContent(Avatar, Asset);
			UpdateLayout.UpdateImmediate(_content);
		}

		private async UniTask FetchAvatar(bool update = false) {
			if (_isLoading) return;
			_isLoading = true;
			Avatar     = await Main.Instance.Network.Fetch(_identifier.ToString());
			_isLoading = false;
			if (update) _component.UpdateContent(Avatar, Asset);
		}

		private async UniTask FetchAsset(bool update = false) {
			if (_isLoading) return;
			_isLoading = true;
			var searchResult = await Main.Instance.Network.SearchAssets(
				_identifier.ToString(),
				new Network.AssetSearchRequest {
					Limit     = 1,
					Versions  = new[] { Version },
					Engines   = new[] { EngineExtensions.CurrentEngine.GetEngineName() },
					Platforms = new[] { PlatformExtensions.CurrentPlatform.GetPlatformName() }
				}
			);
			Asset      = searchResult?.GetAssets()?.FirstOrDefault();
			_isLoading = false;
			if (update) _component.UpdateContent(Avatar, Asset);
		}

		public void RemoveDownload() {
			if (!InCache() && !IsDownloading().Item1) {
				Logger.LogWarning("Cannot remove download, asset is not in cache.");
				return;
			}

			Main.Instance.RemoveFromCache(Asset.GetHash());
			Logger.Log($"Removed asset from cache: {Asset.GetHash()}");
		}

		public void CancelDownload()
			=> GetDownload()?.Cancel();

		public void DownloadAsset() {
			if (IsDownloading().Item1) {
				Logger.Log("Asset is already downloading, no need to start again.");
				return;
			}

			if (InCache()) {
				Logger.Log("Asset is already in cache, no need to download.");
				return;
			}

			var cache = Main.Instance
				.DownloadToCache(Asset.GetUrl(), Asset.GetHash());

			cache.Start().Forget();
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(MId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			(_content, _component) = AvatarComponent.Generate(this, parent);
			_component.UpdateLoading();
			return _content;
		}

		public void OnOpen(IPage lastPage) {
			_events = new[] {
				Main.Instance.CoreAPI.EventAPI.Subscribe("avatar_cache_added", OnCacheUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("avatar_cache_download", OnCacheUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("avatar_cache_removed", OnCacheUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("user_update", OnUserUpdate),
				Main.Instance.CoreAPI.EventAPI.Subscribe("controller_avatar_changed", OnAvatarChanged),
			};
		}

		private void OnUserUpdate(EventData context) { }

		private void OnAvatarChanged(EventData context)
			=> _component?.UpdateSelectButton();

		private void OnCacheUpdate(EventData context)
			=> _component.UpdateDownloading(IsDownloading());

		public void OnDisplay(IPage lastPage) {
			if (Avatar != null) _component.UpdateContent(Avatar, Asset);
			else if (_isLoading) _component.UpdateLoading();
			else _component.UpdateError("Avatar not found or loading failed.");
		}

		public void OnRemove() {
			foreach (var ev in _events)
				Main.Instance.CoreAPI.EventAPI.Unsubscribe(ev);
			CancelDownload();
		}

		public bool InCache()
			=> Asset != null && Main.Instance.HasInCache(Asset.GetHash());

		private Caching.Cache GetDownload()
			=> Asset != null
				? Main.Instance.Cache.GetDownload(Asset.GetUrl(), Asset.GetHash())
				: null;

		public (bool, float) IsDownloading() {
			var cache = GetDownload();
			if (cache == null) return (false, 0f);
			return cache.IsRunning()
				? (true, cache.GetProgress())
				: (false, 1f);
		}
	}
}