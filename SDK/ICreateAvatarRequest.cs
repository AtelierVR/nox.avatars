namespace Nox.Avatars {
	public interface ICreateAvatarRequest {
		/// <summary>
		/// Sets the ID of the avatar to create.
		/// If the ID is 0, a new ID will be generated.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public ICreateAvatarRequest SetId(uint i);

		/// <summary>
		/// Sets the title of the avatar to create.
		/// If the title is null or empty, a default title will be used.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public ICreateAvatarRequest SetTitle(string t);

		/// <summary>
		/// Sets the description of the avatar to create.
		/// If the description is null or empty, no description will be set.
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public ICreateAvatarRequest SetDescription(string d);

		/// <summary>
		/// Sets the thumbnail of the avatar to create.
		/// If the thumbnail is null or empty, no thumbnail will be set.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public ICreateAvatarRequest SetThumbnail(string t);

		/// <summary>
		/// Get the ID of the avatar to create.
		/// </summary>
		/// <returns></returns>
		public uint GetId();

		/// <summary>
		/// Gets the title of the avatar to create.
		/// </summary>
		/// <returns></returns>
		public string GetTitle();
		
		/// <summary>
		/// Gets the description of the avatar to create.
		/// </summary>
		/// <returns></returns>
		public string GetDescription();
		
		/// <summary>
		/// Gets the thumbnail of the avatar to create.
		/// </summary>
		/// <returns></returns>
		public string GetThumbnail();
	}
}