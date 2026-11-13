using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Parameters;
using Nox.CCK;
using Nox.CCK.Avatars;
using Nox.CCK.Build;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Editor {
	public class PlayModeAvatar : MonoBehaviour, IRuntimeAvatar, IRemoveOnBuild {
		public virtual string Id
			=> this.GetId().ToString();

		public virtual Dictionary<string, object> Arguments
			=> new() {
				["source"] = this,
				["local"] = true
			};

		// ReSharper disable Unity.PerformanceAnalysis
		/// <summary>
		/// Descriptor of the avatar being played. A derived component may load the avatar from an
		/// AssetBundle and return the descriptor of the loaded instance instead of its own
		/// GameObject.
		/// </summary>
		public virtual IAvatarDescriptor Descriptor
			=> GetComponent<IAvatarDescriptor>();

		private IAvatarModuleBus _bus;

		public IAvatarModuleBus Bus
			=> _bus ??= new AvatarModuleBus(this);

		public Identifier Identifier { get; set; } = Identifier.Invalid;

		public Identifier Owner { get; set; } = Identifier.Invalid;

		private ISavableContainer _data;

		/// <summary>The play mode avatar is the local player's.</summary>
		public bool IsLocal
			=> true;

		/// <summary>The editor avatar lives as long as the scene: nothing is started for it that must be cancelled.</summary>
		public CancellationToken Token
			=> CancellationToken.None;

		/// <summary>Values of the avatar, kept in memory: the play mode avatar persists nothing.</summary>
		public ISavableContainer Data
			=> _data ??= new MemoryDataContainer();

		public virtual async UniTask Dispose()
			=> await UniTask.Yield();

		/// <summary>
		/// Loads the avatar before it is prepared. The base implementation does nothing: the
		/// descriptor is expected to be on this GameObject. A derived component can load an
		/// AssetBundle here and expose the loaded descriptor through <see cref="Descriptor"/>.
		/// </summary>
		/// <returns><c>false</c> when the avatar could not be loaded.</returns>
		protected virtual UniTask<bool> LoadAsync(CancellationToken token)
			=> UniTask.FromResult(true);

		private void Start()
			=> StartAsync().Forget();

		private async UniTask StartAsync() {
			if (!await LoadAsync(CancellationToken.None)) {
				Logger.LogError("Avatar loading failed, destroying avatar.");
				enabled = false;
				return;
			}

			var descriptor = Descriptor;
			if (descriptor == null) {
				Logger.LogError("AvatarDescriptor component missing, destroying avatar.");
				enabled = false;
				return;
			}

			Logger.Log("Avatar starting...");

			var animator = descriptor.Animator;
			if (!animator) {
				Logger.LogError("Animator component missing in avatar descriptor, destroying avatar.");
				enabled = false;
				return;
			}

			var rigBuilder = animator?.GetComponent<RigBuilder>();
			bool wasRigBuilderEnabled = false;
			if (rigBuilder != null) {
				wasRigBuilderEnabled = rigBuilder.enabled;
				rigBuilder.enabled = false;
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

				if (parameters != null)
					foreach (var param in parameters.GetParameters()) {
						if (param.Name.EndsWith("/PositionWeight")) {
							param.Value = 0.0f;
							continue;
						}

						switch (param.Name) {
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

							case "Tracking/LeftHand/Active":
							case "Tracking/RightHand/Active":
							case "Tracking/Head/Active":
							case "Tracking/LeftFoot/Active":
							case "Tracking/RightFoot/Active":
							case "Tracking/LeftToes/Active":
							case "Tracking/RightToes/Active":
								param.Value = false;
								break;
						}
					}
			} finally {
				if (rigBuilder != null && wasRigBuilderEnabled) {
					await UniTask.Yield();
					rigBuilder.enabled = true;
					rigBuilder.Build();
				}
			}

			await UniTask.DelayFrame(2);
			var rigParameters = descriptor.GetModules<IParameterModule>().FirstOrDefault();
			if (rigParameters != null)
				foreach (var param in rigParameters.GetParameters())
					if (param.Name.EndsWith("/PositionWeight"))
						param.Value = 0.0f;
		}
	}
}
