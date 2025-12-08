using System;
using UnityEngine;

namespace Nox.CCK.Avatars {
	[Serializable]
	public class AvatarCollider {
		public AvatarColliderType type = AvatarColliderType.Automatic;
		public HumanBodyBones     rig;
		public Transform          transform;
		public float              radius;
		public float              height;
		public Vector3            offset;
		public Quaternion         rotation;
	}
}