using System.Collections.Generic;

namespace Nox.Avatars {
	/// <summary>
	/// Mutable <see cref="IBusContext"/> the sender of an event fills in: values are keyed by a string and
	/// read back by the receivers through <see cref="IBusContext.TryGet{T}"/> or the indexer.
	/// <para>
	/// Built with an initializer: <c>bus.Send("scale.change", new BusContext { ["scale"] = scale })</c>.
	/// </para>
	/// </summary>
	public sealed class BusContext : IBusContext {
		/// <summary>Shared context carrying nothing, for events that have no value to pass.</summary>
		public static readonly IBusContext Empty = new BusContext();

		private readonly Dictionary<string, object> _values = new();

		public BusContext() { }

		public BusContext(IDictionary<string, object> values) {
			if (values == null)
				return;

			foreach (var pair in values)
				this[pair.Key] = pair.Value;
		}

		/// <summary>
		/// Reads, writes or removes the value of <paramref name="key"/>; assigning <c>null</c> removes it.
		/// </summary>
		public object this[string key] {
			get => key != null && _values.TryGetValue(key, out var value) ? value : null;
			set {
				if (string.IsNullOrEmpty(key))
					return;

				if (value == null)
					_values.Remove(key);
				else
					_values[key] = value;
			}
		}

		/// <summary>Fluent write, e.g. <c>new BusContext().Set("scale", scale)</c>.</summary>
		public BusContext Set(string key, object value) {
			this[key] = value;
			return this;
		}

		public bool TryGet<T>(string key, out T value) {
			if (this[key] is T typed) {
				value = typed;
				return true;
			}

			value = default;
			return false;
		}
	}
}
