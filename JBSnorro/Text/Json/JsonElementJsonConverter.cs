using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json
{
	/// <summary> Can read and write <see cref="System.Text.Json.JsonElement"/>. </summary>
	public class JsonElementJsonConverter : JsonConverter<JsonElement>
	{
		public static JsonElementJsonConverter Instance { get; } = new JsonElementJsonConverter();
		public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return JsonDocument.ParseValue(ref reader).RootElement;
		}

		public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
		{
			value.WriteTo(writer);
		}
	}
	public static class JsonElementExtensions
	{
		public static MemoryStream ToStream(this JsonElement element)
		{
			var memory = new MemoryStream();
			var writer = new Utf8JsonWriter(memory);
			element.WriteTo(writer);
			memory.Position = 0;
			return memory;
		}
		public static MemoryStream ToStream(this JsonDocument element)
		{
			var memory = new MemoryStream();
			var writer = new Utf8JsonWriter(memory);
			element.WriteTo(writer);
			memory.Position = 0;
			return memory;
		}
		readonly static FieldInfo _parentField = typeof(JsonElement).GetField("_parent", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new UnreachableException();
		public static JsonDocument GetParent(this JsonElement element)
		{
			return (JsonDocument?)_parentField.GetValue(element) ?? throw new InvalidOperationException("JsonElement does not have a parent");
		}
		public static T Deserialize<T>(this JsonElement element, JsonSerializerOptions? options = null)
		{
			using (var stream = element.GetParent().ToStream())
			{
				var reader = new Utf8JsonReader(stream.AsSpan());
				return JsonSerializer.Deserialize<T>(ref reader, options)!;
			}
		}
	}
}
