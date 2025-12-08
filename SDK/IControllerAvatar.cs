using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Nox.Avatars.Controllers {
	public interface IControllerAvatar {
		/// <summary>
		/// Get the avatar associated with the controller.
		/// </summary>
		/// <returns></returns>
		public IRuntimeAvatar GetAvatar();

		/// <summary>
		/// Request an avatar change for the controller's player.
		/// If the change is successful, the controller's avatar will be updated.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="progress"></param>
		public UniTask<IRuntimeAvatar> SetAvatar(IAvatarIdentifier identifier, Action<string, float> progress = null);

		/// <summary>
		/// Set the current avatar directly.
		/// </summary>
		/// <param name="runtimeAvatar"></param>
		/// <returns></returns>
		public UniTask<bool> SetAvatar(IRuntimeAvatar runtimeAvatar);
	}
}