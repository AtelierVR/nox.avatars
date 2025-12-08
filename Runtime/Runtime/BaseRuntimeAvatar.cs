using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using UnityEngine;

namespace Nox.Avatars.Runtime {
	public abstract class BaseRuntimeAvatar : IRuntimeAvatar {
		public IAvatarDescriptor          Descriptor;
		public IAvatarIdentifier          Identifier;
		public string                     Id;
		public GameObject                 Root;
		public Dictionary<string, object> Arguments;

		public virtual string GetId()
			=> Id;

		public Dictionary<string, object> GetArguments()
			=> Arguments ??= new Dictionary<string, object>();

		public virtual IAvatarDescriptor GetDescriptor()
			=> Descriptor;

		public IAvatarIdentifier GetIdentifier()
			=> Identifier;

		public void SetIdentifier(IAvatarIdentifier identifier)
			=> Identifier = identifier;

		public abstract UniTask Dispose();
	}
}