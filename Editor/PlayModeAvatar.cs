using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars;
using Nox.CCK.Build;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Editor {
	[RequireComponent(typeof(IAvatarDescriptor))]
	public class PlayModeAvatar : MonoBehaviour, IRuntimeAvatar, IRemoveOnBuild {
		public string Id
			=> GetEntityId().GetHashCode().ToString();

		public Dictionary<string, object> Arguments
			=> new() {
				["source"] = this,
				["local"]  = true
			};

		// ReSharper disable Unity.PerformanceAnalysis
		public IAvatarDescriptor Descriptor
			=> GetComponent<IAvatarDescriptor>();

		public Identifier Identifier { get; set; } = Identifier.Invalid;

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
			var animator = descriptor.Animator;
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
				if (parameters != null)
					foreach (var param in parameters.GetParameters())
						switch (param.Name)
        				{
        				    case "IsLocal":
        				    case "Grounded":
        				        param.Value = true;
        				        break;
	
        				    case "Upright":
        				        param.Value = 1.0f;
        				        break;
	
        				    case "VRMode":
        				        param.Value = 0;
        				        break;
	
        				    case "UseXR":
        				        param.Value = false;
        				        break;
	
        				    case "TrackingType":
        				        param.Value = 3;
        				        break;
	
        				    case "tracking/left_hand/active":
        				    case "tracking/right_hand/active":
        				    case "tracking/head/active":
        				    case "tracking/left_foot/active":
        				    case "tracking/right_foot/active":
        				    case "tracking/left_toes/active":
        				    case "tracking/right_toes/active":
        				        param.Value = false;
        				        break;
        				}
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