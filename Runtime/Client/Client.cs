using System;
using System.Collections.Generic;
using Nox.Avatars.Runtime.client;
using Nox.Avatars.Runtime.radial;
using Nox.Avatars.Runtime.widget;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;
using Nox.Controllers;
using Nox.Avatars.Controllers;
using UnityEngine.Events;

namespace Nox.Avatars.Runtime {

    public class Client : IClientModInitializer {
        /// <summary>
        /// Avatar runtime actuellement porté par le contrôleur local (peut être null).
        /// </summary>
        public static IRuntimeAvatar CurrentAvatar
            => ControllerAPI?.Current is IControllerAvatar c 
                ? c.GetAvatar() 
                : null;

        /// <summary>
        /// Levé quand l'avatar courant du contrôleur change (avec le nouvel avatar runtime).
        /// </summary>
        public static readonly UnityEvent<IRuntimeAvatar> AvatarChanged = new();
    
        internal static IUiAPI UiAPI
            => Main.Instance.CoreAPI.ModAPI
                   .GetMod("ui")
                   ?.GetInstance<IUiAPI>();

        internal static IControllerAPI ControllerAPI
            => Main.Instance.CoreAPI.ModAPI
                   .GetMod("controllers")
                   ?.GetInstance<IControllerAPI>();

        public static T GetAsset<T>(string path)
            where T : UnityEngine.Object
            => Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(path);

        public static UniTask<T> GetAssetAsync<T>(string path)
            where T : UnityEngine.Object
            => Main.Instance.CoreAPI.AssetAPI.GetAssetAsync<T>(path);

        private EventSubscription[] _events = Array.Empty<EventSubscription>();

        internal static Client Instance;
        internal IClientModCoreAPI CoreAPI;

        public void OnInitializeClient(IClientModCoreAPI api) {
            Instance = this;
            CoreAPI = api;
            _events = new[] {
                CoreAPI.EventAPI.Subscribe("menu_goto", OnGoto),
                CoreAPI.EventAPI.Subscribe("widget_request", OnWidgetRequest),
                CoreAPI.EventAPI.Subscribe("controller_avatar_changed", OnControllerAvatarChanged),
                CoreAPI.EventAPI.Subscribe("radial_goto", OnRadialGoto)
            };
        }

        private void OnControllerAvatarChanged(EventData context) {
            if (!context.TryGet(1, out IRuntimeAvatar runtime))
                return;
            AvatarChanged?.Invoke(runtime);
        }

        private void OnRadialGoto(EventData context) {
            if (!context.TryGet(0, out int mid)) return;
            if (!context.TryGet(1, out string path)) return;
            var page = AvatarRadialPage.Create(path);
            if (page == null) return;
            Main.Instance.CoreAPI.EventAPI.Emit("radial_display", mid, page);
        }

        private void OnGoto(EventData context) {
            if (!context.TryGet(0, out int mid)) return;
            if (!context.TryGet(1, out string key)) return;
            var menu = UiAPI?.Get<IMenu>(mid);
            if (menu == null) return;
            IPage page = null;
            if (AvatarPage.GetStaticKey() == key) 
                page = AvatarPage.OnGotoAction(menu, context.Data[2..]);
            if (page == null) return;
            Main.Instance.CoreAPI.EventAPI.Emit("menu_display", menu.Id, page);
        }

        private void OnWidgetRequest(EventData context) {
            if (!context.TryGet(0, out int mid)) return;
            if (!context.TryGet(1, out RectTransform tr)) return;
            var menu = UiAPI?.Get<IMenu>(mid);
            if (menu == null) return;
            List<(GameObject, IWidget)> widgets = new();
            if (AvatarWidget.TryMake(menu, tr, out var widget)) widgets.Add(widget);
            foreach (var value in widgets) context.Callback(value.Item2, value.Item1);
        }

        public void OnDisposeClient() {
            foreach (var e in _events) CoreAPI.EventAPI.Unsubscribe(e);
            _events = Array.Empty<EventSubscription>();
            CoreAPI = null;
            Instance = null;
        }
    }

}