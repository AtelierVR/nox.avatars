using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace Nox.Avatars.Players {
	public interface IPlayerAvatar {
		/// <summary>
		/// Get the current avatar of the player.
		/// </summary>
		/// <returns></returns>
		public Identifier GetAvatar();

		/// <summary>
		/// Set the avatar of the player.
		/// </summary>
		/// <param name="identifier"></param>
		public UniTask<bool> SetAvatar(Identifier identifier);
	}
}