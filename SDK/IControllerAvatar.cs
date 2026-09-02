using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

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
		public UniTask<IRuntimeAvatar> SetAvatar(Identifier identifier, Action<string, float> progress = null);

		/// <summary>
		/// Set the current avatar directly.
		/// </summary>
		/// <param name="runtimeAvatar"></param>
		/// <returns></returns>
		public UniTask<bool> SetAvatar(IRuntimeAvatar runtimeAvatar);

		/// <summary>
		/// Reload the current avatar of the controller.
		/// </summary>
		/// <param name="progress"></param>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> ReloadAvatar(Action<string, float> progress = null);
	}
}