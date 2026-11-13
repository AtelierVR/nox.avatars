using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Events;

namespace Nox.CCK.Avatars {
	/// <summary>
	/// Minimal in-memory <see cref="ISavableContainer"/>: the values live as long as the object does and are
	/// never persisted. Used by the runtime avatars that have no storage of their own (the play mode
	/// avatar); the persistent one belongs to the avatars runtime (see <c>BaseRuntimeAvatar.Data</c>).
	/// </summary>
	public sealed class MemoryDataContainer : ISavableContainer {
		/// <summary>Separator of the key segments: no path can collide with another one.</summary>
		private const char Separator = '\u001F';

		private readonly Dictionary<string, object> _values = new();

		/// <inheritdoc />
		public NoxEvent<string[], object, object> OnChanged { get; } = new();

		/// <summary>Nothing to read: the values only live in memory.</summary>
		public UniTask Load()
			=> UniTask.CompletedTask;

		/// <summary>Nothing to write: the values only live in memory.</summary>
		public UniTask Save()
			=> UniTask.CompletedTask;

		/// <inheritdoc />
		public T Get<T>(string[] key, T @default = default)
			=> _values.TryGetValue(Key(key), out var value) && value is T typed
				? typed
				: @default;

		/// <inheritdoc />
		public T Get<T>(string key, T @default = default)
			=> Get(new[] { key }, @default);

		/// <inheritdoc />
		public bool Has(string[] key)
			=> _values.ContainsKey(Key(key));

		/// <inheritdoc />
		public bool Has(string key)
			=> Has(new[] { key });

		/// <inheritdoc />
		public void Set<T>(string[] key, T value) {
			if (key == null || key.Length == 0)
				return;

			var name = Key(key);
			_values.TryGetValue(name, out var previous);
			_values[name] = value;
			OnChanged.Invoke(key, value, previous);
		}

		/// <inheritdoc />
		public void Set<T>(string key, T value)
			=> Set(new[] { key }, value);

		/// <inheritdoc />
		public void Delete(string[] key) {
			if (key == null || key.Length == 0)
				return;

			var name = Key(key);
			if (!_values.Remove(name, out var previous))
				return;

			OnChanged.Invoke(key, null, previous);
		}

		/// <inheritdoc />
		public void Delete(string key)
			=> Delete(new[] { key });

		/// <inheritdoc />
		public void Dispose() { }

		private static string Key(string[] key)
			=> string.Join(Separator, key);
	}
}
