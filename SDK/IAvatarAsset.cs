namespace Nox.Avatars {
	public interface IAvatarAsset {
		public uint Id { get; }

		public ushort Version { get; }

		public string Engine { get; }

		public string Platform { get; }

		public bool IsEmpty { get; }

		public string Url { get; }

		public string[] Features { get; }

		public string Hash { get; }

		public long Size { get; }
	}
}