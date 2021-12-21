using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json
{
	/// <summary> Serializes values of the specified enum type by name and deserializes them from string (case-insensitive). </summary>
	public class EnumStringJsonConverter<T> : JsonConverter<T> where T : Enum
	{
		public static EnumStringJsonConverter<T> Instance { get; } = new EnumStringJsonConverter<T>();

		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using var _ = this.DetectStackoverflow(reader, typeToConvert);
			string name = JsonSerializer.Deserialize<string>(ref reader, options);
			return this.Parse(name);
		}

		protected virtual T Parse(string name)
		{
			if (Enum.TryParse(typeof(T), name, ignoreCase: true, out object result))
				return (T)result;
			if (int.TryParse(name, out int i))
				return (T)(object)i;
			throw new JsonException($"Could not convert '{name}' to enum type '{typeof(T).FullName}'");
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			string name = this.GetName(value, options);
			writer.WriteStringValue(name);
		}

		/// <summary> Converts the name of the enum for serialization. </summary>
		protected virtual string GetName(T value, JsonSerializerOptions options)
		{
			return Enum.GetName(typeof(T), value);
		}
	}
	/// <summary> Serializes values of the specified enum type by name to lowercase and deserializes them from string (case-insensitive). </summary>
	public class LowerCaseEnumStringJsonConverter<T> : EnumStringJsonConverter<T> where T : Enum
	{
		/// <inheritdoc/>
		protected override string GetName(T value, JsonSerializerOptions options)
		{
			return base.GetName(value, options).ToLowerInvariant();
		}
	}
}
