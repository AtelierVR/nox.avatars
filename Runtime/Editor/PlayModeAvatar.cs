using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Parameters;
using Nox.CCK.Avatars;
using Nox.CCK.Build;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime.Editor {
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
			} catch (System.Exception ex) {
				Logger.LogError($"Exception during avatar setup: {ex}");
				enabled = false;
			}
		}
	}
}