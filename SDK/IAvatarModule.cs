using Cysharp.Threading.Tasks;

namespace Nox.Avatars {
	public interface IAvatarModule {
		public UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar);
	}
}