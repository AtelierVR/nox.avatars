using Nox.Assets.Editor;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using IPanel = Nox.Editor.Panel.IPanel;

namespace Nox.Avatars.Runtime.Editor {
	/// <summary>
	/// Registers the avatar publisher panel. The panel itself is the generic one from
	/// <c>nox.assets</c>; this class only hands it the <see cref="AvatarPublisherSource"/>, i.e. the
	/// avatar's instructions (build, rules, version).
	/// </summary>
	public class PublisherPanel : AssetPublisherPanel, IEditorModInitializer, IPanel {
		public static PublisherPanel Panel { get; private set; }

		internal IEditorModCoreAPI API;

		public PublisherPanel() : base(new AvatarPublisherSource()) { }

		public void OnInitializeEditor(IEditorModCoreAPI api) {
			API = api;
			Panel = this;
			Source.Initialize(api);
		}

		public void OnDisposeEditor() {
			API = null;
			Panel = null;
		}
	}
}
