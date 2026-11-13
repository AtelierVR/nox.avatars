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
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.Editor {
	[RequireComponent(typeof(IAvatarDescriptor))]
	public class PlayModeAvatar : MonoBehaviour, IRuntimeAvatar, IRemoveOnBuild {
		public string Id
			=> this.GetId().ToString();

		public Dictionary<string, object> Arguments
			=> new() {
				["source"] = this,
				["local"]  = true
			};

		// ReSharper disable Unity.PerformanceAnalysis
		public IAvatarDescriptor Descriptor
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
			} catch (System.Exception ex) {
				Logger.LogError($"Exception during avatar setup: {ex}");
				enabled = false;
			}
		}
	}
}
