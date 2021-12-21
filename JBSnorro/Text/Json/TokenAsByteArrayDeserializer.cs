using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json
{
	public class TokenAsByteArrayDeserializer : JsonConverter<byte[]>
	{
		public static TokenAsByteArrayDeserializer Instance { get; } = new TokenAsByteArrayDeserializer();
		public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return reader.GetTokenAsByteArray();
		}

		public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options) => throw new NotImplementedException();
	}
}
