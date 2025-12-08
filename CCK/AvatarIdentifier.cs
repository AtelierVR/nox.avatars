using System;
using System.Collections.Generic;
using Nox.Avatars;

namespace Nox.CCK.Avatars {
	/// <summary>
	/// Implementation of IAvatarIdentifier
	/// </summary>
	public class AvatarIdentifier : IAvatarIdentifier {
		public const uint   InvalidId   = 0;
		public const string LocalServer = "::";
		public const ushort Latest      = ushort.MaxValue;

		private readonly uint                         _id;
		private readonly string                       _server;
		private readonly Dictionary<string, string[]> _metadata;

		public AvatarIdentifier(uint id, Dictionary<string, string[]> meta = null, string server = LocalServer) {
			_id       = id;
			_server   = server;
			_metadata = meta ?? new Dictionary<string, string[]>();
		}

		/// <summary>
		/// Check if the identifier is valid (not equal to InvalidId)
		/// </summary>
		/// <returns></returns>
		public bool IsValid()
			=> _id != InvalidId;

		/// <summary>
		/// Check if the identifier points to the current/local server
		/// </summary>
		/// <returns></returns>
		public bool IsLocal()
			=> _server == LocalServer || string.IsNullOrEmpty(_server);

		/// <summary>
		/// Get the avatar ID, or InvalidId if the identifier is not valid
		/// </summary>
		/// <returns></returns>
		public uint GetId()
			=> IsValid() ? _id : InvalidId;


		/// <summary>
		/// Get the string representation of the identifier
		/// </summary>
		/// <param name="fallbackServer"></param>
		/// <returns></returns>
		public string ToString(string fallbackServer = null)
			=> $"{_id.ToString()}{(IsLocal() ? string.IsNullOrEmpty(fallbackServer) ? "" : "@" + fallbackServer : "@" + _server)}";

		/// <summary>
		/// Get the server address
		/// </summary>
		/// <returns></returns>
		public string GetServer()
			=> _server;

		/// <summary>
		/// Get the metadata dictionary
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, string[]> GetMetadata()
			=> _metadata;

		/// <summary>
		/// Create an AvatarIdentifier from a base IAvatarIdentifier
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public static AvatarIdentifier From(IAvatarIdentifier identifier) {
			if (identifier == null) return null;
			return new AvatarIdentifier(
				identifier.GetId(),
				identifier.GetMetadata(),
				identifier.GetServer()
			);
		}

		/// <summary>
		/// Create an AvatarIdentifier from a string representation
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public static AvatarIdentifier From(string identifier) {
			if (string.IsNullOrEmpty(identifier))
				return null;

			var parts = identifier.Split('@');
			switch (parts.Length) {
				case > 2:
					return null;
				case 1:
					parts = new[] { parts[0], null };
					break;
			}

			if (string.IsNullOrEmpty(parts[1]))
				parts[1] = LocalServer;

			var split = parts[0].Split('?');
			if (split.Length > 2)
				return null;
			var idPart = split[0];
			if (!uint.TryParse(idPart, out var id))
				return null;
			var metadata = new Dictionary<string, string[]>();
			if (split.Length != 2)
				return new AvatarIdentifier(id, metadata, parts[1]);

			var metaParts = split[1].Split('&');
			foreach (var part in metaParts) {
				var metaSplit = part.Split('=');
				if (metaSplit.Length < 1)
					continue;
				var key   = metaSplit[0];
				var value = metaSplit.Length > 1 ? string.Join("=", metaSplit, 1, metaSplit.Length - 1) : null;
				if (string.IsNullOrEmpty(key))
					continue;
				if (metadata.TryGetValue(key, out var values)) {
					var newValues = new string[values.Length + 1];
					values.CopyTo(newValues, 0);
					newValues[^1] = value;
					metadata[key] = newValues;
				} else metadata[key] = new[] { value };
			}

			return new AvatarIdentifier(id, metadata, parts[1]);
		}

		/// <summary>
		/// Get the avatar version from metadata, or Latest if not specified
		/// </summary>
		/// <returns></returns>
		public ushort GetVersion() {
			if (_metadata.TryGetValue("v", out var versions) && versions.Length > 0 && ushort.TryParse(versions[0], out var version))
				return version;
			return Latest;
		}

		/// <summary>
		/// Set the avatar version in metadata
		/// </summary>
		/// <param name="version"></param>
		public void SetVersion(ushort version) {
			_metadata.Remove("v");
			if (version == Latest) return;
			_metadata["v"] = new[] { version.ToString() };
		}

		/// <summary>
		/// Check equality with another object
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
			=> obj is IAvatarIdentifier identifier && Equals(identifier);

		/// <summary>
		/// Get the hash code of the identifier
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
			=> HashCode.Combine(GetId(), GetServer());

		/// <summary>
		/// Check equality with another IAvatarIdentifier
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		private bool Equals(IAvatarIdentifier other) {
			if (other == null) return false;
			if (ReferenceEquals(this, other)) return true;
			return GetHashCode() == other.GetHashCode();
		}
	}
}