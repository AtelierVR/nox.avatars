using Cysharp.Threading.Tasks;

namespace Nox.Avatars.Players {
	public interface IPlayerAvatar {
		/// <summary>
		/// Get the current avatar of the player.
		/// </summary>
		/// <returns></returns>
		public IAvatarIdentifier GetAvatar();

		/// <summary>
		/// Set the avatar of the player.
		/// </summary>
		/// <param name="identifier"></param>
		public UniTask<bool> SetAvatar(IAvatarIdentifier identifier);
	}
}