using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Assets.Editor;
using Nox.Avatars.Editor;
using Nox.CCK.Avatars;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using Nox.Network.Assets;
using UnityEditor;
using UnityEngine;

namespace Nox.Avatars.Runtime.Editor {
	/// <summary>
	/// What an avatar declares to the generic builder: the descriptor it builds, its platforms, how
	/// to build it and what to do around the build. The panel itself — selector, notifications,
	/// platforms, output folder, progress and result — comes from <c>nox.assets</c>.
	/// </summary>
	public sealed class AvatarBuilderSource : AssetBuilderSource {
		public override string   Panel => "avatar";
		public override string   Label => "Avatar";
		public override string[] Path  => new[] { "avatar", "builder" };
		public override string   Title => "Avatar Builder";

		public override Type DescriptorType
			=> typeof(AvatarDescriptor);

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

		public override Platform[] Platforms {
			get => AvatarDescriptorHelper.CurrentAvatar?.Targets ?? Array.Empty<Platform>();
			set {
				var live = AvatarDescriptorHelper.Live(AvatarDescriptorHelper.CurrentAvatar);

				if (!live)
					return;

				live.Targets = value;
				EditorUtility.SetDirty(live);
			}
		}

		public override string DefaultOutputFolder
			=> "Assets/AvatarBuilds/";

		public override Func<AssetBuildContext, UniTask<AssetBuildResult>> Build
			=> BuildAsync;

		/// <summary>Called after a build: the scenes may have been reloaded.</summary>
		public override Action OnBuildFinished
			=> AvatarDescriptorHelper.Rebind;

		/// <summary>Opens the publisher panel, as the previous builder did.</summary>
		public override IToolOption[] Options(IWindow window)
			=> new IToolOption[] {
				new DefaultToolOption("Publisher", () => {
					// The publisher panel is ours: use its static reference rather than asking the ModAPI for
					// our own mod (which logs an unrelated-module warning).
					var panel = PublisherPanel.Panel;

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

			var build = await Nox.Avatars.Pipeline.Builder.Build(new Nox.Avatars.Pipeline.BuildData {
				Descriptor       = descriptor,
				OutputPath       = context.OutputPath,
				ProgressCallback = context.Report
			});

			if (build == null)
				return AssetBuildResult.Failed("The avatar build returned no result.");

			if (build.IsFailed)
				return AssetBuildResult.Failed(build.Message);

			var outputs = new Dictionary<Platform, string>();

			foreach (var output in build.Outputs)
				if (!string.IsNullOrEmpty(output.Path))
					outputs[output.Platform] = output.Path;

			return AssetBuildResult.Ok(outputs);
		}
	}
}
