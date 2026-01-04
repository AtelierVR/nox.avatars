using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Controllers;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nox.Avatars.Runtime.client {

    public class AvatarComponent : MonoBehaviour {
        public GameObject withThumbnail;
        public GameObject withoutThumbnail;
        public Image thumbnail;
        public TextLanguage title;
        public TextLanguage identifier;
        public TextLanguage label;
        public RectTransform content;
        public AvatarPage Page;
        private CancellationTokenSource _thumbnailTokenSource;
        public GameObject descriptionContainer;
        public TextLanguage descriptionText;
        public RectTransform actions;


        #region Select Logic

        public Image selectIcon;
        public Button selectButton;
        public TextLanguage selectLabel;

        private bool IsCurrentAvatar() {
            var controller = Main.Instance.ControllerAPI?.Current;
            if (controller is not IControllerAvatar ca || Page?.Avatar == null) return false;

            var currentAvatar = ca.GetAvatar();
            if (currentAvatar == null) return false;

            return currentAvatar.GetIdentifier()
                                ?.Equals(Page.Avatar.GetIdentifier())
                ?? false;
        }

        public void UpdateSelectButton() {
            var isSelected = IsCurrentAvatar();
            selectButton.interactable = !isSelected;
            selectIcon.sprite = Client.GetAsset<Sprite>(
                isSelected
                    ? "ui:icons/selected.png"
                    : "ui:icons/select.png"
            );
        }

        private async UniTask OnSelectClickedAsync() {
            if (!selectButton.interactable || Page?.Avatar == null) return;

            selectButton.interactable = false;
            var controller = Main.Instance.ControllerAPI?.Current;

            if (controller is IControllerAvatar ca) {
                var id = Page.Avatar.GetIdentifier();
                await UniTask.WhenAll(
                    Main.Instance.UserAPI.UpdateCurrent(Main.Instance.UserAPI.MakeUpdateCurrentRequest().SetAvatar(id.ToString())),
                    ca.SetAvatar(id)
                );
            }

            UpdateSelectButton();
        }

        #endregion


        #region Favorite Logic

        private bool _isFavorite = false;
        private bool _isFavoriteHover = false;
        public Image favoriteIcon;
        public Button favoriteButton;
        public TextLanguage favoriteLabel;

        #endregion


        #region Cache Logic

        private bool _isCachedHover = false;
        private string _lastTextureCaching;
        public Image cacheIcon;
        public Button cacheButton;
        public Slider cacheProgress;
        public TextLanguage cacheLabel;

        #endregion


        public void UpdateError(string error) {
            title.UpdateText("avatar.error");
            identifier.UpdateText("avatar.error");
            label.UpdateText("avatar.error");
            thumbnail.sprite = null;
            withThumbnail.SetActive(false);
            withoutThumbnail.SetActive(true);
            descriptionContainer.SetActive(false);
        }

        public void UpdateLoading() {
            title.UpdateText("avatar.loading");
            identifier.UpdateText("avatar.loading");
            label.UpdateText("avatar.loading");
            thumbnail.sprite = null;
            withThumbnail.SetActive(false);
            withoutThumbnail.SetActive(true);
            descriptionContainer.SetActive(false);
        }

        public void UpdateContent(IAvatar avatar, IAvatarAsset asset) {
            if (avatar == null) return;

            title.UpdateText("avatar.title", new[] { avatar.GetTitle() });
            label.UpdateText("avatar.about.title", new[] { avatar.GetTitle() ?? avatar.GetIdentifier().ToString() });
            identifier.UpdateText(
                "avatar.identifier", new[] { avatar.GetIdentifier().ToString(), avatar.GetId().ToString(), avatar.GetServerAddress() }
            );

            if (!string.IsNullOrEmpty(avatar.GetDescription())) {
                descriptionText.UpdateText("avatar.description", new[] { avatar.GetDescription() });
                descriptionContainer.SetActive(true);
            } else descriptionContainer.SetActive(false);

            UpdateThumbnail(avatar).Forget();
            UpdateFavoriteState().Forget();

            HoverCache(_isCachedHover);
            HoverFavorite(_isFavoriteHover);
            UpdateSelectButton();
        }

        private async UniTask UpdateThumbnail(IAvatar avatar) {
            if (_thumbnailTokenSource != null) {
                _thumbnailTokenSource?.Cancel();
                _thumbnailTokenSource?.Dispose();
            }

            _thumbnailTokenSource = new CancellationTokenSource();
            if (avatar?.GetThumbnailUrl() != null) {
                var texture = await Main.Instance.NetworkAPI
                                        .FetchTexture(avatar.GetThumbnailUrl())
                                        .AttachExternalCancellation(_thumbnailTokenSource.Token);
                if (texture) {
                    thumbnail.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    withThumbnail.SetActive(true);
                    withoutThumbnail.SetActive(false);
                } else {
                    thumbnail.sprite = null;
                    withThumbnail.SetActive(false);
                    withoutThumbnail.SetActive(true);
                }
            } else {
                thumbnail.sprite = null;
                withThumbnail.SetActive(false);
                withoutThumbnail.SetActive(true);
            }

            _thumbnailTokenSource = null;
        }


        #region Favorite Logic

        private async UniTask UpdateFavoriteState() {
            var favorites = await Main.Instance.Network.FetchFavorites();
            _isFavorite = favorites.Any(f => f.Equals(Page.Avatar.GetIdentifier()));
            HoverFavorite(_isFavoriteHover);
        }

        private void HoverFavorite(bool isHover) {
            _isFavoriteHover = isHover;
            favoriteIcon.sprite = Client.GetAsset<Sprite>(
                $"ui:icons/{(isHover ? _isFavorite ? "bookmark_remove" : "bookmark_add" : _isFavorite ? "bookmark_star" : "bookmark")}.png"
            );
            favoriteLabel.UpdateText(
                isHover
                    ? _isFavorite
                        ? "avatar.favorite.remove"
                        : "avatar.favorite.add"
                    : _isFavorite
                        ? "avatar.favorite.star"
                        : "avatar.favorite.none"
            );
        }

        private async UniTask OnFavoriteClickedAsync() {
            if (!favoriteButton.interactable) return;

            _isFavorite = !_isFavorite;
            favoriteButton.interactable = false;
            HoverFavorite(_isFavoriteHover);
            var id = Page.Avatar.GetIdentifier();

            var favorites = _isFavorite
                ? await Main.Instance.Network.AddFavorite(id.ToString())
                : await Main.Instance.Network.RemoveFavorite(id.ToString());

            _isFavorite = favorites.Any(f => f.Equals(id));
            HoverFavorite(_isFavoriteHover);

            favoriteButton.interactable = true;
        }

        public void UpdateFavorite(bool isFavorite) {
            _isFavorite = isFavorite;
            HoverFavorite(_isFavoriteHover);
        }

        #endregion


        #region Cache Logic

        public void UpdateDownloading((bool, float) download) {
            if (download.Item1) {
                cacheProgress.value = download.Item2;
            } else cacheProgress.value = 0;

            HoverCache(_isCachedHover);
        }

        private void HoverCache(bool isHover) {
            _isCachedHover = isHover;
            var texture = ((Page.InCache() ? 1 : 0) << 2)
                | ((Page.IsDownloading().Item1 ? 1 : 0) << 1)
                | ((_isCachedHover ? 1 : 0) << 0);
            if (texture > 5) texture -= 4;
            if (!Page.IsDownloading().Item1) cacheProgress.value = 0;

            var tex = $"icons/cache{texture}.png";

            if (_lastTextureCaching != tex) cacheIcon.sprite = Client.GetAsset<Sprite>(_lastTextureCaching = tex);

            cacheLabel.UpdateText(
                "avatar.cache."
                + new[] { "none", "add", "downloading", "cancel", "downloaded", "remove" }[texture]
            );
        }

        private void OnCacheClicked() {
            if (Page.IsDownloading().Item1) {
                Page.CancelDownload();
                return;
            }

            if (Page.InCache()) {
                Page.RemoveDownload();
                return;
            }

            Page.DownloadAsset();
            HoverCache(_isCachedHover);
        }

        #endregion


        private static void SetupEvents(EventTrigger trigger, UnityEngine.Events.UnityAction onClick, UnityEngine.Events.UnityAction onEnter, UnityEngine.Events.UnityAction onExit) {
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => onClick?.Invoke());
            trigger.triggers.Add(entry);

            entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => onEnter?.Invoke());
            trigger.triggers.Add(entry);

            entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entry.callback.AddListener(_ => onExit?.Invoke());
            trigger.triggers.Add(entry);
        }

        public static (GameObject, AvatarComponent) Generate(AvatarPage avatarPage, RectTransform parent) {
            var content = Object.Instantiate(Client.GetAsset<GameObject>("ui:prefabs/split.prefab"), parent);

            var component = content.AddComponent<AvatarComponent>();
            component.Page = avatarPage;
            content.name = $"[{avatarPage.GetKey()}_{content.GetInstanceID()}]";

            var splitContent = Reference.GetComponent<RectTransform>("content", content);
            var containerAsset = Client.GetAsset<GameObject>("ui:prefabs/container.prefab");

            // generate profile
            var container = Object.Instantiate(containerAsset, splitContent);
            var profile = Object.Instantiate(
                Client.GetAsset<GameObject>("ui:prefabs/profile.prefab"),
                Reference.GetComponent<RectTransform>("content", container)
            );
            component.identifier = Reference.GetComponent<TextLanguage>("identifier", profile);
            component.title = Reference.GetComponent<TextLanguage>("title", profile);
            component.thumbnail = Reference.GetComponent<Image>("thumbnail", profile);
            component.withThumbnail = Reference.GetReference("with_thumbnail", profile);
            component.withoutThumbnail = Reference.GetReference("without_thumbnail", profile);

            // generate dashboard
            container = Object.Instantiate(Client.GetAsset<GameObject>("ui:prefabs/container_full.prefab"), splitContent);

            var withTitleAsset = Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab");
            var scrollAsset = Client.GetAsset<GameObject>("ui:prefabs/scroll.prefab");
            var listAsset = Client.GetAsset<GameObject>("ui:prefabs/list.prefab");
            var boxAsset = Client.GetAsset<GameObject>("ui:prefabs/box.prefab");

            var withTitle = Object.Instantiate(
                withTitleAsset,
                Reference.GetComponent<RectTransform>("content", container)
            );

            var header = Reference.GetReference("header", withTitle);
            var iconAsset = Client.GetAsset<GameObject>("ui:prefabs/header_icon.prefab");
            var labelAsset = Client.GetAsset<GameObject>("ui:prefabs/header_label.prefab");
            var icon = Object.Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
            var labelObj = Object.Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

            Reference.GetComponent<Image>("image", icon).sprite = Client.GetAsset<Sprite>("ui:icons/avatar.png");
            component.label = Reference.GetComponent<TextLanguage>("text", labelObj);
            component.label.UpdateText("avatar.about.title");

            var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
            // setup scroll + list
            var scroll = Object.Instantiate(scrollAsset, contentDash);
            var list = Object.Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
            component.content = Reference.GetComponent<RectTransform>("content", list);

            // add box actions
            var boxActions = Object.Instantiate(boxAsset, component.content);
            Reference.GetComponent<TextLanguage>("text", boxActions).UpdateText("avatar.about.actions");
            var actionContainerAsset = Client.GetAsset<GameObject>("ui:prefabs/action_container.prefab");
            component.actions = Reference.GetComponent<RectTransform>("content", Object.Instantiate(actionContainerAsset, Reference.GetComponent<RectTransform>("content", boxActions)));

            // Generate action buttons
            GenerateActions(component);

            // add box description
            component.descriptionContainer = Object.Instantiate(boxAsset, component.content);
            Reference.GetComponent<TextLanguage>("text", component.descriptionContainer).UpdateText("avatar.about.description");
            component.descriptionText = Reference.GetComponent<TextLanguage>(
                "text", Object.Instantiate(
                    Client.GetAsset<GameObject>("ui:prefabs/text.prefab"),
                    Reference.GetComponent<RectTransform>("content", component.descriptionContainer)
                )
            );

            return (content, component);
        }

        private static void GenerateActions(AvatarComponent component) {
            var actionButtonAsset = Client.GetAsset<GameObject>("ui:prefabs/action_button.prefab");


            #region Select Button

            var select = Object.Instantiate(actionButtonAsset, component.actions);
            var selectEventTrigger = Reference.GetComponent<EventTrigger>("button", select);
            component.selectButton = Reference.GetComponent<Button>("button", select);
            component.selectIcon = Reference.GetComponent<Image>("image", select);
            component.selectLabel = Reference.GetComponent<TextLanguage>("text", select);
            component.selectLabel.UpdateText("avatar.select");
            component.selectIcon.sprite = Client.GetAsset<Sprite>("ui:icons/select.png");
            SetupEvents(
                selectEventTrigger,
                () => component.OnSelectClickedAsync().Forget(),
                null,
                null
            );

            #endregion


            #region Cache Button

            var cache = Object.Instantiate(actionButtonAsset, component.actions);
            var cacheEventTrigger = Reference.GetComponent<EventTrigger>("button", cache);
            component.cacheButton = Reference.GetComponent<Button>("button", cache);
            component.cacheIcon = Reference.GetComponent<Image>("image", cache);
            component.cacheLabel = Reference.GetComponent<TextLanguage>("text", cache);
            component.cacheProgress = Reference.GetComponent<Slider>("progress", cache);
            component.cacheLabel.UpdateText("avatar.cache.none");
            component.cacheIcon.sprite = Client.GetAsset<Sprite>("ui:icons/cache0.png");
            SetupEvents(
                cacheEventTrigger,
                () => component.OnCacheClicked(),
                () => component.HoverCache(true),
                () => component.HoverCache(false)
            );

            #endregion


            #region Favorite Button

            var favorite = Object.Instantiate(actionButtonAsset, component.actions);
            var favoriteEventTrigger = Reference.GetComponent<EventTrigger>("button", favorite);
            component.favoriteButton = Reference.GetComponent<Button>("button", favorite);
            component.favoriteIcon = Reference.GetComponent<Image>("image", favorite);
            component.favoriteLabel = Reference.GetComponent<TextLanguage>("text", favorite);
            component.favoriteLabel.UpdateText("avatar.favorite.none");
            component.favoriteIcon.sprite = Client.GetAsset<Sprite>("ui:icons/bookmark.png");
            SetupEvents(
                favoriteEventTrigger,
                () => component.OnFavoriteClickedAsync().Forget(),
                () => component.HoverFavorite(true),
                () => component.HoverFavorite(false)
            );

            #endregion


        }
    }

}