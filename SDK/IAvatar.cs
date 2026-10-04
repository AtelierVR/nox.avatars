using Nox.Network.Assets;

namespace Nox.Avatars {
	/// <summary>
	/// An avatar published on a node: the generic asset (<see cref="IAsset"/>) plus the values that
	/// only an avatar carries. Title, description, owner, contributors, tags, alias, release,
	/// images and dates all come from <see cref="IAsset"/>. Avatars currently add no field of
	/// their own, so the interface only narrows <see cref="IAsset"/> to the avatar type.
	/// </summary>
	public interface IAvatar : IAsset { }
}