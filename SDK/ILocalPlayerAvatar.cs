using Cysharp.Threading.Tasks;

namespace Nox.Avatars.Players {
	public interface ILocalPlayerAvatar : IPlayerAvatar {
		public UniTask<bool> OnAvatarReady();
		public UniTask<bool> OnAvatarFailed(string reason);
	}
}