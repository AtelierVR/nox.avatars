using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.Avatars {
	/// <summary>
	/// Interface for avatar modules,
	/// which can be used to add additional functionality to avatars.
	/// </summary>
	public interface IAvatarModule {
		/// <summary>
		/// The priority of the module,
		/// which determines the order in which modules are initialized.
		/// </summary>
		public int Priority { get; }

		/// <summary>
		/// Initializes the module with the given runtime avatar.
		/// Called once per phase in order: Pre → Init → Post.
		/// </summary>
		/// <param name="runtimeAvatar"></param>
		/// <param name="phase"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public UniTask<bool> Setup(IRuntimeAvatar runtimeAvatar, AvatarModulePhase phase, CancellationToken token = default);
	}
}