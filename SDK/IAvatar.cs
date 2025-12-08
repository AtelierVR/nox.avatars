using System;

namespace Nox.Avatars {
	public interface IAvatar {
		public uint GetId();

		public string GetTitle();

		public string GetServerAddress();

		public string GetDescription();

		public string GetThumbnailUrl();

		public string[] GetTags();

		public string GetOwnerId();

		public DateTime GetCreatedAt();

		public DateTime GetUpdatedAt();

		public string GetServer();

		public IAvatarIdentifier GetIdentifier();
	}
}