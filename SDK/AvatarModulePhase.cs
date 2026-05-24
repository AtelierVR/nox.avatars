namespace Nox.Avatars {
	/// <summary>
	/// Represents the phase of the avatar module setup process.
	/// </summary>
	public enum AvatarModulePhase {
		/// <summary>Pre-initialization phase: lightweight state preparation.</summary>
		Pre,
		/// <summary>Main initialization phase: all heavy setup work happens here.</summary>
		Init,
		/// <summary>Post-initialization phase: runs after all modules have completed Init.</summary>
		Post,
	}
}