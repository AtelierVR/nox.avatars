using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.CCK;
using Nox.CCK.Events;

namespace Nox.Avatars.Runtime {
	/// <summary>
	/// JSON-backed <see cref="ISavableContainer"/> of a runtime avatar: the values (saved parameter profiles,
	/// ...) are kept in memory, and <see cref="Save"/> hands the whole document to the avatar it belongs to.
	/// <para>
	/// A key is a path of segments, like a config path: <c>{ "profiles", "casual" }</c> reads
	/// <c>profiles.casual</c> in the stored JSON.
	/// </para>
	/// </summary>
	public sealed class AvatarDataContainer : ISavableContainer {
		private JObject _json = new();

		/// <summary>
		/// Avatar holding this container: it owns the storage (the table of the account), the container only
		/// carries the values. Without it the container is memory-only.
		/// </summary>
		private readonly BaseRuntimeAvatar _avatar;

		public AvatarDataContainer(BaseRuntimeAvatar avatar = null)
			=> _avatar = avatar;

		/// <inheritdoc />
		public UniTask Load()
			=> _avatar != null
				? _avatar.LoadDataAsync()
				: UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask Save()
			=> _avatar != null
				? _avatar.SaveDataAsync()
				: UniTask.CompletedTask;

		/// <inheritdoc />
		public NoxEvent<string[], object, object> OnChanged { get; } = new();

		/// <inheritdoc />
		public T Get<T>(string[] key, T @default = default) {
			var token = Resolve(key);
			if (token == null || token.Type == JTokenType.Null)
				return @default;

			try {
				return token.ToObject<T>() ?? @default;
			} catch (Exception) {
				return @default;
			}
		}

		/// <inheritdoc />
		public T Get<T>(string key, T @default = default)
			=> Get(new[] { key }, @default);

		/// <inheritdoc />
		public bool Has(string[] key)
			=> Resolve(key) != null;

		/// <inheritdoc />
		public bool Has(string key)
			=> Has(new[] { key });

		/// <inheritdoc />
		public void Set<T>(string[] key, T value) {
			if (key == null || key.Length == 0)
				return;

			var previous = Resolve(key);
			var next     = value == null ? JValue.CreateNull() : JToken.FromObject(value);

			var current = _json;
			for (var i = 0; i < key.Length - 1; i++) {
				if (current[key[i]] is not JObject child) {
					child         = new JObject();
					current[key[i]] = child;
				}
				current = child;
			}

			current[key[^1]] = next;
			OnChanged.Invoke(key, value, previous?.ToObject<object>());
		}

		/// <inheritdoc />
		public void Set<T>(string key, T value)
			=> Set(new[] { key }, value);

		/// <inheritdoc />
		public void Delete(string[] key) {
			if (key == null || key.Length == 0)
				return;

			var previous = Resolve(key);

			var current = _json;
			for (var i = 0; i < key.Length - 1; i++) {
				if (current[key[i]] is not JObject child)
					return;
				current = child;
			}

			if (current.Remove(key[^1]))
				OnChanged.Invoke(key, null, previous?.ToObject<object>());
		}

		/// <inheritdoc />
		public void Delete(string key)
			=> Delete(new[] { key });

		/// <inheritdoc />
		public void Dispose() { }

		/// <summary>The whole document, as stored.</summary>
		internal string ToJson()
			=> _json.ToString(Formatting.None);

		/// <summary>Replaces the document with <paramref name="json"/> (unusable JSON keeps an empty one).</summary>
		internal void Load(string json) {
			try {
				_json = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
			} catch (Exception) {
				_json = new JObject();
			}
		}

		/// <summary>Token stored at <paramref name="key"/>, or null when any of its segments is missing.</summary>
		private JToken Resolve(string[] key) {
			JToken current = _json;

			foreach (var segment in key) {
				if (current is not JObject obj)
					return null;

				current = obj[segment];
				if (current == null)
					return null;
			}

			return current;
		}
	}
}
