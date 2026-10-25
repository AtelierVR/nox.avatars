using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Assets.Editor;
using Nox.Avatars.Editor;
using Nox.Avatars.Pipeline;
using Nox.CCK.Avatars;
using Nox.CCK.Convertors;
using Nox.CCK.Network.Assets;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using Nox.Network.Assets;
using UnityEditor;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.Editor {
	/// <summary>
	/// What an avatar declares to the generic publisher: where it lives, how to build it, who may
	/// publish it and the version stored in the descriptor. Everything else — the form, the attach
	/// flow, the images, the release and the uploads — comes from <c>nox.assets</c>.
	/// </summary>
	public sealed class AvatarPublisherSource : AssetPublisherSource {
		public override string   Panel  => "avatar";
		public override string   Label  => "Avatar";
		public override string[] Path   => new[] { "avatar", "publisher" };
		public override string   Title  => "Avatar Publisher";
		public override AssetEndpoint Endpoint => AvatarsEndpoint.Endpoint;

		public override Type DescriptorType => typeof(AvatarDescriptor);

		// --- selection ---------------------------------------------------------------------

		public override UnityEngine.Object Selection {
			get => AvatarDescriptorHelper.CurrentAvatar;
			set {
				if (value is AvatarDescriptor descriptor)
					AvatarDescriptorHelper.SetCurrentAvatar(descriptor);
			}
		}

		public override UnityEngine.Object[] Candidates()
			=> AvatarDescriptorHelper.Candidates();

		public override string Describe(UnityEngine.Object candidate)
			=> candidate is AvatarDescriptor descriptor && !string.IsNullOrEmpty(descriptor.gameObject.scene.name)
				? $"{descriptor.name} ({descriptor.gameObject.scene.name})"
				: base.Describe(candidate);

		// --- version -----------------------------------------------------------------------

		public override ushort Version {
			get => AvatarDescriptorHelper.CurrentAvatar?.publishVersion ?? 0;
			set {
				var live = AvatarDescriptorHelper.Live(AvatarDescriptorHelper.CurrentAvatar);

				if (!live)
					return;

				live.publishVersion = value;
				EditorUtility.SetDirty(live);
			}
		}

		// --- attached asset ----------------------------------------------------------------

		public override async UniTask<IAsset> AttachAsync(uint id) {
			var descriptor = AvatarDescriptorHelper.CurrentAvatar;
			var assets     = Nox.Assets.Editor.Editor.Pipeline;
			if (descriptor == null || assets == null)
				return null;

			var server = UserServer();

			if (string.IsNullOrEmpty(server)) {
				Logger.OpenDialog("Error", "No server address available.", "Ok");
				return null;
			}

			Network.AvatarAsset asset = null;

			if (id > 0)
				asset = await assets.Fetch<Network.AvatarAsset>(server, Endpoint, id.ToString());

			if (asset == null)
				asset = await assets.Create<Network.AvatarAsset>(server, Endpoint, new AssetCreateRequest { Id = id });

			if (asset == null) {
				Logger.OpenDialog("Error", "Failed to create or find the avatar.", "Ok");
				return null;
			}

			var user = Nox.Avatars.Runtime.Main.UserAPI?.Current;

			if (user == null || !asset.IsContributor(user.Identifier)) {
				Logger.OpenDialog("Error", "You are not a contributor of this avatar.", "Ok");
				SetAttached(null);
				return null;
			}

			var live = AvatarDescriptorHelper.Live(descriptor);

			if (live) {
				live.publishId     = asset.Id;
				live.publishServer = asset.Server;
				EditorUtility.SetDirty(live);
			}

			SetAttached(asset);
			return asset;
		}

		/// <summary>
		/// Reopens the avatar that was published before: the descriptor stores its id and server, so the
		/// form attaches it silently instead of showing the « Attach » prompt.
		/// </summary>
		public override async UniTask<bool> TryAutoAttachAsync() {
			var descriptor = AvatarDescriptorHelper.CurrentAvatar;
			var assets     = Nox.Assets.Editor.Editor.Pipeline;

			if (descriptor == null || assets == null || descriptor.publishId == 0)
				return false;

			var server = !string.IsNullOrEmpty(descriptor.publishServer)
				? descriptor.publishServer
				: UserServer();

			if (string.IsNullOrEmpty(server))
				return false;

			var asset = await assets.Fetch<Network.AvatarAsset>(server, Endpoint, descriptor.publishId.ToString());

			if (asset == null)
				return false;

			var user = Nox.Avatars.Runtime.Main.UserAPI?.Current;

			if (user == null || !asset.IsContributor(user.Identifier))
				return false;

			SetAttached(asset);
			return true;
		}

		public override async UniTask<bool> UpdateAsync(string title, string description) {
			var assets = Nox.Assets.Editor.Editor.Pipeline;

			if (assets == null || Attached == null)
				return false;

			var updated = await assets.Update(
				Attached.Server,
				Endpoint,
				AssetRef,
				new AssetUpdateRequest {
					Title       = title.ToTranslated(),
					Description = description.ToTranslated()
				}
			);

			if (updated == null)
				return false;

			SetAttached(updated);
			return true;
		}

		public override void Detach() {
			var live = AvatarDescriptorHelper.Live(AvatarDescriptorHelper.CurrentAvatar);

			if (live) {
				live.publishId     = 0;
				live.publishServer = string.Empty;
				EditorUtility.SetDirty(live);
			}

			SetAttached(null);
		}

		// --- publish -----------------------------------------------------------------------

		public override IAssetBuilder Builder
			=> new DelegateAssetBuilder(BuildAsync, "avatar");

		public override AssetPublishConstraints Constraints
			=> new AssetPublishRules {
				PlatformsFunc = request => (request.State as AvatarDescriptor)?.Targets is { Length: > 0 } targets
					? targets
					: new[] { PlatformExtensions.CurrentPlatform },
				OwnershipFunc = (_, asset) => {
					var user = Nox.Avatars.Runtime.Main.UserAPI?.Current;

					if (asset == null)
						return "Failed to verify the avatar.";
					if (user == null)
						return "You are not logged in.";

					return asset.IsContributor(user.Identifier) ? null : "You are not a contributor of this avatar.";
				},
				PrepareFunc = (request, version) => WritePublishVersion(request, version)
			};

		/// <summary>Opens the builder panel of the avatar, as the previous publisher did.</summary>
		public override IToolOption[] Options(IWindow window)
			=> new IToolOption[] {
				new DefaultToolOption("Builder", () => {
					// The builder panel is ours: use its static reference rather than asking the ModAPI for
					// our own mod (which logs an unrelated-module warning).
					var panel = BuilderPanel.Panel;

					if (panel == null)
						return;

					window.SetActive(panel);
					window.Repaint();
				})
			};

		/// <summary>Builds one bundle per platform of the descriptor.</summary>
		private static async UniTask<AssetBuildResult> BuildAsync(AssetBuildContext context) {
			if (context?.State is not AvatarDescriptor descriptor)
				return AssetBuildResult.Failed("No avatar descriptor to build.");

			var build = await Nox.Avatars.Pipeline.Builder.Build(new BuildData {
				Descriptor       = descriptor,
				OutputPath       = context.OutputPath,
				ProgressCallback = context.Report
			});

			if (build == null)
				return AssetBuildResult.Failed("The avatar build returned no result.");

			if (build.Type != BuildResultType.Success)
				return AssetBuildResult.Failed(build.Message);

			var outputs = new Dictionary<Platform, string>();

			foreach (var output in build.Outputs)
				if (!string.IsNullOrEmpty(output.Path))
					outputs[output.Platform] = output.Path;

			return AssetBuildResult.Ok(outputs);
		}

		/// <summary>Writes the version to the live descriptor before the build reads it.</summary>
		private static string WritePublishVersion(AssetPublishRequest request, ushort version) {
			var live = AvatarDescriptorHelper.Live(request?.State as AvatarDescriptor);

			if (live) {
				live.publishVersion = version;
				EditorUtility.SetDirty(live);
			}

			return null;
		}

		private static string UserServer()
			=> Nox.Avatars.Runtime.Main.UserAPI?.Current?.Server;
	}
}
