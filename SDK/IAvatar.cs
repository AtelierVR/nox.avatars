using System;
using Nox.CCK.Utils;
using Nox.Users;

namespace Nox.Avatars {
	public interface IAvatar {
		/// <summary>
		/// The unique id of the avatar.
		/// </summary>
		public uint Id { get; }

		/// <summary>
		/// The title of the avatar.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// The owner of the avatar.
		/// </summary>
		Identifier Owner { get; }

		/// <summary>
		/// The server where the avatar is hosted.
		/// </summary>
		public string Server { get; }

		/// <summary>
		/// The description of the avatar.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// The thumbnail of the avatar.
		/// </summary>
		public string Thumbnail { get; }

		/// <summary>
		/// The tags of the avatar.
		/// </summary>
		public string[] Tags { get; }
		
		/// <summary>
		/// Is a <see cref="ushort"/> of the <see cref="IAvatarAsset.Version"/>.
		/// It is <see cref="ushort.MaxValue"/> when the avatar has no assets.
		/// </summary>
		public ushort Release { get; }

		/// <summary>
		/// The date and time when the avatar was created.
		/// </summary>
		public DateTime CreatedAt { get; }

		/// <summary>
		/// The unique identifier of the avatar,
		/// which can be used to fetch its assets.
		/// </summary>
		public Identifier Identifier { get; }
	}
}