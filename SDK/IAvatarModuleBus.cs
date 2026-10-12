namespace Nox.Avatars {
	/// <summary>
	/// Per-avatar channel the modules use to talk to each other without referencing each other: a module
	/// sends a named event and every module of the same avatar receives it through
	/// <see cref="IAvatarModule.OnBusEvent"/>, so a module can react to a sibling's work without a
	/// dependency on that sibling.
	/// </summary>
	public interface IAvatarModuleBus {
		/// <summary>
		/// Broadcasts an event to every module of the avatar (see <see cref="IAvatarModule.OnBusEvent"/>).
		/// </summary>
		/// <param name="name">Name of the event, e.g. <c>"scale.change"</c>.</param>
		/// <param name="context">Values carried by the event, may be <c>null</c> (see <see cref="IBusContext"/>).</param>
		void Send(string name, IBusContext context = null);
	}
}
