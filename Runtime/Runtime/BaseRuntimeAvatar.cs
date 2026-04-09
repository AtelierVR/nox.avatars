using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Avatars.Runtime {
	public abstract class BaseRuntimeAvatar : IRuntimeAvatar {
		protected GameObject Root;

		public virtual string Id { get; internal set; } = null;

		public Dictionary<string, object> Arguments { get; internal set; } = new();

		public virtual IAvatarDescriptor Descriptor { get; internal set; }

		public Identifier Identifier { get; set; } = Identifier.Invalid;

		public abstract UniTask Dispose();
	}
}