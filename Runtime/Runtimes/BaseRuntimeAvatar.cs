using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Avatars.Runtime {
	public abstract class BaseRuntimeAvatar : IRuntimeAvatar {
		protected GameObject Root;

		private IAvatarModuleBus _bus;

		public virtual string Id { get; protected internal set; } = null;

		public Dictionary<string, object> Arguments { get; protected internal set; } = new();

		public virtual IAvatarDescriptor Descriptor { get; protected internal set; }

		public IAvatarModuleBus Bus
			=> _bus ??= new AvatarModuleBus(this);

		public Identifier Identifier { get; set; } = Identifier.Invalid;

		public abstract UniTask Dispose();
	}
}