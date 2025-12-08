namespace Nox.Avatar {
	public interface IUpdateAvatarRequest {
		/// <summary>
		/// Sets the title of the world.
		/// If the title is empty, no change will be made.
		/// If the title is null, the current title will be removed.
		/// Any other value will set the title to the given value.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public IUpdateAvatarRequest SetTitle(string t);

		/// <summary>
		/// Sets the description of the world.
		/// If the description is empty, no change will be made.
		/// If the description is null, the current description will be removed.
		/// Any other value will set the description to the given value.
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public IUpdateAvatarRequest SetDescription(string d);

		/// <summary>
		/// Sets the thumbnail for the world.
		/// If the thumbnail is empty, no change will be made.
		/// If the thumbnail is null, the current thumbnail will be removed.
		/// Any other value will set the thumbnail to the given value.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public IUpdateAvatarRequest SetThumbnail(string i);

		/// <summary>
		/// Gets the title of the world.
		/// </summary>
		/// <returns></returns>
		public string GetTitle();

		/// <summary>
		/// Gets the description of the world.
		/// </summary>
		/// <returns></returns>
		public string GetDescription();

		/// <summary>
		/// Gets the thumbnail of the world.
		/// </summary>
		/// <returns></returns>
		public string GetThumbnail();
	}
}