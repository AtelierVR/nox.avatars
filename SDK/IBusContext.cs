namespace Nox.Avatars {
	/// <summary>
	/// Payload carried by an event sent on the avatar's module bus (see <see cref="IAvatarModuleBus.Send"/>):
	/// a small bag of keyed values the receiver reads back with <see cref="TryGet{T}"/> or the indexer.
	/// <para>
	/// The keys are owned by the module that sends the event — <c>scale.change</c> carries a <c>scale</c>
	/// float — so a module can react to a sibling's event without a dependency on that sibling.
	/// </para>
	/// </summary>
	public interface IBusContext {
		/// <summary>
		/// Reads the value stored under <paramref name="key"/> as <typeparamref name="T"/>. Returns
		/// <c>false</c> when the key is absent or holds another type, leaving <paramref name="value"/> at its
		/// default.
		/// </summary>
		bool TryGet<T>(string key, out T value);

		/// <summary>
		/// Value stored under <paramref name="key"/>, or <c>null</c> when the key is absent (see
		/// <see cref="TryGet{T}"/> for a typed read).
		/// </summary>
		object this[string key] { get; }
	}
}
