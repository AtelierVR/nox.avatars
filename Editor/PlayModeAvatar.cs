using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Editor {
	[RequireComponent(typeof(IAvatarDescriptor))]
	public class PlayModeAvatar : MonoBehaviour, IRuntimeAvatar, IRemoveOnBuild {
		public string GetId()
			=> GetInstanceID().ToString();

		public Dictionary<string, object> GetArguments()
			=> new() {
				["source"] = this,
				["local"]  = true
			};

		// ReSharper disable Unity.PerformanceAnalysis
		public IAvatarDescriptor GetDescriptor()
			=> GetComponent<IAvatarDescriptor>();

		public IAvatarIdentifier GetIdentifier()
			=> null;

		public void SetIdentifier(IAvatarIdentifier identifier)
			=> Logger.LogWarning("PlayModeAvatar does not support setting an identifier.");


		public async UniTask Dispose()
			=> await UniTask.Yield();

		private void Start()
			=> StartAsync().Forget();

		private async UniTask StartAsync() {
			var descriptor = GetComponent<IAvatarDescriptor>();
			if (descriptor == null) {
				Logger.LogError("AvatarDescriptor component missing, destroying avatar.");
				enabled = false;
				return;
			}

			Logger.Log("Avatar starting...");

			// Temporarily disable rigging to prevent TransformStreamHandle errors during setup
			var animator = descriptor.GetAnimator();
			if (!animator) {
				Logger.LogError("Animator component missing in avatar descriptor, destroying avatar.");
				enabled = false;
				return;
			}

			var  rigBuilder           = animator?.GetComponent<RigBuilder>();
			bool wasRigBuilderEnabled = false;
			if (rigBuilder != null) {
				wasRigBuilderEnabled = rigBuilder.enabled;
				rigBuilder.enabled   = false;
			}

			try {
				if (!await AvatarSetup.Prepare(this)) {
					Logger.LogError("Avatar preparation failed, destroying avatar.");
					enabled = false;
					return;
				}

				Logger.Log("Avatar prepared successfully.");

				var parameters = descriptor
					.GetModules<IParameterModule>()
					.FirstOrDefault();

				// Set parameters without triggering rigging builds
				parameters?.GetParameter("IsLocal")?.Set(true);
				parameters?.GetParameter("Grounded")?.Set(true);
				parameters?.GetParameter("Upright")?.Set(1.0f);
				parameters?.GetParameter("VRMode")?.Set(0);
				parameters?.GetParameter("UseXR")?.Set(false);
				parameters?.GetParameter("TrackingType")?.Set(3);
				parameters?.GetParameter("tracking/left_hand/active")?.Set(false);
				parameters?.GetParameter("tracking/right_hand/active")?.Set(false);
				parameters?.GetParameter("tracking/head/active")?.Set(false);
				parameters?.GetParameter("tracking/left_foot/active")?.Set(false);
				parameters?.GetParameter("tracking/right_foot/active")?.Set(false);
				parameters?.GetParameter("tracking/left_toes/active")?.Set(false);
				parameters?.GetParameter("tracking/right_toes/active")?.Set(false);
			} finally {
				// Re-enable rigging after setup is complete and wait a frame
				if (rigBuilder != null && wasRigBuilderEnabled) {
					await UniTask.Yield();
					rigBuilder.enabled = true;
					// Build the rigging safely after everything is set up
					rigBuilder.Build();
				}
			}
		}
	}
}