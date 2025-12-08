using System;
using Nox.Avatars;
using Nox.CCK.Avatars;
using Nox.CCK.Utils;

namespace Nox.Avatars.Runtime.Network {
	// ReSharper disable InconsistentNaming
	[Serializable]
	public class Avatar : IAvatar, INoxObject {
		public uint     id;
		public string   title;
		public string   description;
		public string   thumbnail;
		public string[] tags;
		public string   owner;
		public string   server;
		public ulong    created_at;
		public ulong    updated_at;

		public uint GetId()
			=> id;

		public string GetTitle()
			=> title;

		public string GetServerAddress()
			=> server;

		public string GetDescription()
			=> description;

		public string GetThumbnailUrl()
			=> thumbnail;

		public string[] GetTags()
			=> tags ??= Array.Empty<string>();

		public string GetOwnerId()
			=> owner;

		public DateTime GetCreatedAt()
			=> DateTimeOffset.FromUnixTimeMilliseconds((long)created_at).UtcDateTime;

		public DateTime GetUpdatedAt()
			=> DateTimeOffset.FromUnixTimeMilliseconds((long)updated_at).UtcDateTime;

		public string GetServer()
			=> server;

		public IAvatarIdentifier GetIdentifier()
			=> new AvatarIdentifier(id, null, server);
	}
}