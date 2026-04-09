using System;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Identifier = Nox.CCK.Utils.Identifier;

namespace Nox.Avatars.Runtime.Network {
	// ReSharper disable InconsistentNaming
	[Serializable]
	public class Avatar : IAvatar {
		[JsonProperty("id")]
		public uint Id { get; private set; }

		[JsonProperty("title")]
		public string Title { get; private set; }

		[JsonProperty("description")]
		public string Description { get; private set; }

		[JsonProperty("thumbnail")]
		public string Thumbnail { get; private set; }

		[JsonProperty("tags")]
		public string[] Tags { get; private set; }

		[JsonProperty("owner"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier Owner { get; private set; }

		[JsonProperty("server")]
		public string Server { get; private set; }

		[JsonProperty("release")]
		public int Release { get; private set; }

		[JsonProperty("created_at"), JsonConverter(typeof(UnixTimestampToDateTimeConverter))]
		public DateTime CreatedAt { get; private set; }

		public Identifier Identifier
			=> new("a", Id, null, Server);

		private class UnixTimestampToDateTimeConverter : JsonConverter<DateTime> {
			public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
				=> writer.WriteValue(new DateTimeOffset(value).ToUnixTimeMilliseconds());

			public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
				=> reader.TokenType switch {
					JsonToken.Integer => DateTimeOffset.FromUnixTimeMilliseconds((long)reader.Value!).UtcDateTime,
					JsonToken.Float   => DateTimeOffset.FromUnixTimeMilliseconds((long)(double)reader.Value!).UtcDateTime,
					_                 => throw new JsonSerializationException("Invalid token type for DateTime")
				};
		}
	}
}