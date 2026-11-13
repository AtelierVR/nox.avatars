using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Avatars.Runtime.Settings;
using Nox.CCK;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Avatars.Runtime {
	public abstract class BaseRuntimeAvatar : IRuntimeAvatar {
		/// <summary>MIME type of the persisted document in the table.</summary>
		private const string DataMime = "application/json+avatar";

		protected GameObject Root;

		private IAvatarModuleBus _bus;
		private AvatarDataContainer _data;
		private Identifier _identifier = Identifier.Invalid;
		private bool _loaded;

		/// <summary>Write in flight (see <see cref="SaveDataAsync"/>): joined instead of duplicated.</summary>
		private UniTask _save = UniTask.CompletedTask;
		private bool   _saving;

		/// <summary>Serialized <see cref="Data"/> as last written, so an unchanged state writes nothing.</summary>
		private string _saved;

		/// <summary>Lifetime of the avatar: cancelled by <see cref="Dispose"/>.</summary>
		private readonly CancellationTokenSource _lifetime = new();

		/// <summary>Cancelled when the avatar goes away (see <see cref="IRuntimeAvatar.Token"/>).</summary>
		public CancellationToken Token
			=> _lifetime.Token;

		public virtual string Id { get; protected internal set; } = null;

		public Dictionary<string, object> Arguments { get; protected internal set; } = new();

		public virtual IAvatarDescriptor Descriptor { get; protected internal set; }

		public IAvatarModuleBus Bus
			=> _bus ??= new AvatarModuleBus(this);

		public Identifier Identifier {
			get => _identifier;
			set {
				if (_identifier.Equals(value))
					return;

				_identifier = value;

				// An avatar is identified only once it is fully loaded: the controller assigns the identifier
				// after the module setup ran, which is why the saved data is read from here rather than then.
				if (CanPersist && !_loaded)
					Data.Load().Forget();
			}
		}

		public Identifier Owner { get; set; } = Identifier.Invalid;

		/// <summary>
		/// Local avatars are the ones loaded with the <c>local</c> argument (see the controllers and the
		/// console); a remote avatar loaded by the relay passes <c>false</c>.
		/// </summary>
		public virtual bool IsLocal
			=> Arguments.TryGetValue("local", out var local)
				&& local is true;

		public ISavableContainer Data
			=> _data ??= new AvatarDataContainer(this);

		/// <summary>
		/// Whether <see cref="Data"/> may be read and written: only a local avatar, and only once it is
		/// identified. The loading, default and error avatars are built from a bundled prefab and never get
		/// an identifier, so they must not create an <c>avatars.0</c> entry.
		/// </summary>
		protected virtual bool CanPersist
			=> IsLocal && Identifier.IsValid();

		/// <summary>
		/// Key of <see cref="Data"/> in the table: <c>avatars.&lt;avatar&gt;</c> (<c>avatars.42</c>).
		/// The table is per account (see <c>nox.table</c>), so neither the owner nor the server belong to
		/// the key: an avatar keeps one entry per account, like the worlds persistence.
		/// </summary>
		internal string DataKey
			=> $"avatars.{Identifier.ToShortString(false)}";

		/// <summary>
		/// Reads <see cref="Data"/> from its storage (see <see cref="ISavableContainer.Load"/>): the local
		/// filesystem table when the player is offline, the account table when connected. Nothing happens for
		/// a remote avatar, nor for one that is still loading or failed to load (see <see cref="CanPersist"/>).
		/// </summary>
		internal async UniTask LoadDataAsync() {
			if (_loaded || !CanPersist)
				return;

			// The `tables` mod initializes before the avatars (see the constraints of nox.mod.json): its
			// instance is there as soon as an avatar is set up.
			var table = Main.Instance?.TableAPI;
			if (table == null || Token.IsCancellationRequested)
				return;

			var local = SaveLocally();

			try {
				var entry = await table.Get(DataKey, local: local);
				if (entry?.AsString != null)
					((AvatarDataContainer)Data).Load(entry.AsString);

				_loaded = true;
				_saved   = entry?.AsString;

				// A load that finds nothing leaves the avatar on its defaults: the outcome belongs in the log,
				// a silent "nothing restored" is impossible to tell from a working persistence.
				if (entry?.AsString == null)
					Logger.LogDebug($"Avatar data '{DataKey}' not found in the {(local ? "local" : "account")} table.", tag: nameof(BaseRuntimeAvatar));
				else
					Logger.LogDebug($"Avatar data '{DataKey}' loaded from the {(local ? "local" : "account")} table ({entry.AsString.Length} bytes).", tag: nameof(BaseRuntimeAvatar));
			} catch (Exception exception) {
				// Not loaded: a later call can try again rather than leaving the avatar on its defaults.
				Logger.LogDebug($"Avatar data '{DataKey}' cannot be loaded: {exception.Message}", tag: nameof(BaseRuntimeAvatar));
			}

			// The modules waited for the data (see IParameterModule): tell them it is there, so they apply it.
			if (_loaded)
				Bus.Send(IRuntimeAvatar.DataLoadedEvent);
		}

		/// <summary>
		/// Writes <see cref="Data"/> to its storage (see <see cref="ISavableContainer.Save"/>). Nothing happens
		/// for a remote avatar, nor for one that is still loading or failed to load (see
		/// <see cref="CanPersist"/>), nor when the data did not move since the last successful write. A write
		/// already in flight is joined instead of duplicated.
		/// </summary>
		internal UniTask SaveDataAsync() {
			// A write already running carries the current data: the caller waits for that one instead of
			// starting a second request that the shutdown could outlive.
			if (_saving)
				return _save;

			_save = WriteAsync();
			return _save;
		}

		private async UniTask WriteAsync() {
			_saving = true;

			try {
				var table = Main.Instance?.TableAPI;
				if (table == null || !CanPersist || _data == null)
					return;

				// Never write before the stored document was read: the avatar would replace it with what it
				// has in memory (nothing), losing the values of the previous session.
				if (!_loaded)
					return;

				var json = _data.ToJson();
				if (json == _saved)
					return;

				// The table reports a failed write with null rather than an exception: the data stays
				// unsaved, so the same values are tried again on the next change or at the shutdown.
				var entry = await table.Set(DataKey, json, DataMime, SaveLocally());
				if (entry != null)
					_saved = json;
			} catch (Exception exception) {
				Logger.LogDebug($"Avatar data '{DataKey}' cannot be saved: {exception.Message}", tag: nameof(BaseRuntimeAvatar));
			} finally {
				_saving = false;
			}
		}

		/// <summary>
		/// Whether the data stays on this machine: the setting asks for a local save (see
		/// <see cref="ParametersSaveSetting"/>), or the player is not connected.
		/// </summary>
		private static bool SaveLocally()
			=> ParametersSaveSetting.PrefersLocal
				|| Main.UserAPI?.Current == null;

		/// <summary>
		/// Writes what the avatar has to persist, then releases it: the write is awaited while the avatar and
		/// its modules are still alive, and only then is it destroyed, so the values of the session survive
		/// the table the mods hold. <see cref="Token"/> is cancelled last.
		/// </summary>
		public async UniTask Dispose() {
			try {
				// Before OnDispose: destroying the avatar drops everything to serialize, and the caller tears
				// the mods down as soon as this returns.
				await Data.Save();
				await OnDispose();
			} finally {
				// Cancelled, not disposed: a module that still holds the token must be able to read it.
				// The data container is kept as-is: a late flush (OnDestroy) must still carry its profiles.
				_lifetime.Cancel();
			}
		}

		/// <summary>Releases the resources of the avatar (see <see cref="Dispose"/>).</summary>
		protected abstract UniTask OnDispose();
	}
}
