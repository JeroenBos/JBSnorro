using JBSnorro;
using JBSnorro.Diagnostics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json
{
	public static class JsonSerializerExtensions
	{
		public static JsonSerializerOptions Clone(this JsonSerializerOptions serializer)
		{
			Contract.Requires(serializer != null);

			var result = new JsonSerializerOptions()
			{
				AllowTrailingCommas = serializer.AllowTrailingCommas,
				DefaultBufferSize = serializer.DefaultBufferSize,
				DictionaryKeyPolicy = serializer.DictionaryKeyPolicy,
				Encoder = serializer.Encoder,
#pragma warning disable CS0618, SYSLIB0020
				IgnoreNullValues = serializer.IgnoreNullValues,
#pragma warning restore CS0618, SYSLIB0020
				IgnoreReadOnlyProperties = serializer.IgnoreReadOnlyProperties,
				MaxDepth = serializer.MaxDepth,
				PropertyNameCaseInsensitive = serializer.PropertyNameCaseInsensitive,
				PropertyNamingPolicy = serializer.PropertyNamingPolicy,
				ReadCommentHandling = serializer.ReadCommentHandling,
				WriteIndented = serializer.WriteIndented,
			};
			foreach (var converter in serializer.Converters)
			{
				result.Converters.Add(converter);
			}
			return result;
		}
		/// <summary> Clones the specified options, but omits the specified converters, if present. </summary>
		public static JsonSerializerOptions CloneWithout(this JsonSerializerOptions options, params JsonConverter[] convertersToRemove)
		{
			Contract.Requires(options != null);
			Contract.Requires(convertersToRemove != null);

			var result = options.Clone();
			foreach (var converterToRemove in convertersToRemove)
				result.Converters.Remove(converterToRemove);
			return result;
		}

		/// <summary>
		/// Gets the next token wrapped in a json reader and progresses the specified reader.
		/// </summary>
		public static Utf8JsonReader GetTokenAsJsonReader(this ref Utf8JsonReader reader)
		{
			// Maybe one day I'll figure out how to not copy. This actually copies twice...
			return new Utf8JsonReader(reader.GetTokenAsByteSpan());
		}
		internal static ReadOnlySpan<byte> GetTokenAsByteSpan(this ref Utf8JsonReader reader)
		{
			// Maybe one day I'll figure out how to not copy. This actually copies twice...
			// however, if the utf8JsonReader is truly forward only, then that shouldn't be possible
			// but I don't know whether than means any copies can remain at a particular state.
			return Encoding.UTF8.GetBytes(reader.GetTokenAsJson()).AsSpan();
		}
		/// <summary>
		/// Gets the next token as json string and progresses the reader.
		/// </summary>
		public static string GetTokenAsJson(this ref Utf8JsonReader reader)
		{
			using var document = JsonDocument.ParseValue(ref reader);
			return document.RootElement.ToString();
		}
		internal static byte[] GetTokenAsByteArray(this Utf8JsonReader reader)
		{
			return Encoding.UTF8.GetBytes(reader.GetTokenAsJson());
		}
		public static void CopyCurrentTokenTo(this Utf8JsonReader reader, Utf8JsonWriter writer)
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.Comment:
					writer.WriteCommentValue(reader.GetComment());
					break;
				case JsonTokenType.EndArray:
					writer.WriteEndArray();
					break;
				case JsonTokenType.EndObject:
					writer.WriteEndObject();
					break;
				case JsonTokenType.False:
					writer.WriteBooleanValue(false);
					break;
				case JsonTokenType.None:
					break;
				case JsonTokenType.Null:
					writer.WriteNullValue();
					break;
				case JsonTokenType.Number:
					if (reader.TryGetDecimal(out decimal d))
						writer.WriteNumberValue(d);
					else
						throw new OverflowException("Too large for decimal"); // How can I copy over the number if it can't even be represented in C#? 
					break;
				case JsonTokenType.PropertyName:
					writer.WritePropertyName(reader.GetString() ?? throw new UnreachableException());
					break;
				case JsonTokenType.StartArray:
					writer.WriteStartArray();
					break;
				case JsonTokenType.StartObject:
					writer.WriteStartObject();
					break;
				case JsonTokenType.String:
					writer.WriteStringValue(reader.GetString());
					break;
				case JsonTokenType.True:
					writer.WriteBooleanValue(true);
					break;
				default:
					throw new DefaultSwitchCaseUnreachableException();
			}
		}
		/// <summary> Advances the reader until the next property. </summary>
		public static void ReadProperty(this ref Utf8JsonReader r)
		{
			Contract.Requires(r.TokenType == JsonTokenType.PropertyName);
			r.Read();
			r.GetTokenAsJson();
		}
		/// <summary> Copies the remainder of the specified reader to the writer. </summary>
		public static void CopyTo(this ref Utf8JsonReader reader, Utf8JsonWriter writer)
		{
			do
			{
				reader.CopyCurrentTokenTo(writer);
			} while (reader.Read());
		}
		/// <summary> Copies the current token of the reader to to the writer, and advances to the next token. </summary>
		public static void ReadTo(this ref Utf8JsonReader reader, Utf8JsonWriter writer)
		{
			reader.CopyCurrentTokenTo(writer);
			reader.Read();
		}
		/// <summary>
		/// Gets whether the converter collection is still mutable (which it is until the first (de)serialization action occurs).
		/// </summary>
		public static bool IsMutable(this JsonSerializerOptions options)
		{
			Contract.Requires(options != null);

			var field = typeof(JsonSerializerOptions).GetField("_haveTypesBeenCreated", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new UnreachableException();
			return !(bool)field.GetValue(options)!;
		}
		///// <summary> Freezes the specified options. </summary>
		//public static void Freeze(this JsonSerializerOptions options)
		//{
		//	Contract.Requires(options != null);

		//	try
		//	{
		//		JsonSerializer.Serialize(null, options);
		//	}
		//	catch (JsonException)
		//	{
		//	}
		//}
		/// <summary> Asserts that the next json token has exactly the same specified properties. </summary>
		public static void AssertHasExactlyTheseProperties(this Utf8JsonReader reader, IEnumerable<string> expectedProperties, JsonSerializerOptions options)
		{
			var comparer = options.PropertyNameCaseInsensitive ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;
			var remainingProperties = new HashSet<string>(expectedProperties, comparer);
			var extraProperties = new List<string>();
			var found = new List<string>();

			// note that reader is a clone anyway so the caller will not be affected by how much the current reader reads
			if (reader.TokenType != JsonTokenType.StartObject)
				if (reader.TokenType == JsonTokenType.StartArray)
					throw new NotImplementedException("Arrays not implemented");
				else
					throw new JsonException("Reader not at start of object");
			reader.Read();

			while (true)
			{
				switch (reader.TokenType)
				{
					case JsonTokenType.EndObject:
						if (remainingProperties.Count == 0 && extraProperties.Count == 0)
							return;
						else
						{
							throw new JsonException(
								$"The following {remainingProperties.Count} properties are missing: {string.Join(", ", remainingProperties)}\n"
								+ $"and the following {extraProperties} properties were extra: {string.Join(", ", extraProperties)}");
						}
					case JsonTokenType.PropertyName:
						string name = reader.GetString() ?? throw new UnreachableException();
						reader.ReadProperty();
						found.Add(name);
						string convertedName = options.PropertyNamingPolicy?.ConvertName(name) ?? name;
						if (remainingProperties.Contains(convertedName))
						{
							remainingProperties.Remove(convertedName);
						}
						else
						{
							extraProperties.Add(name);
						}
						reader.Read();
						break;

					default:
						throw new JsonException("Unexpected json token");
				}
			}


		}
		private static readonly MethodInfo JsonConverter_1_Write = typeof(JsonConverter<>).GetMethod("Write")!;
		public static void Write(this JsonConverter converter, Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
		{
			if (converter == null)
				throw new ArgumentNullException();

			var type = converter.GetType();
			if (typeof(JsonConverter<>).IsAssignableFrom(type))
			{
				// var mi = type.GetMethods()
				//     		 .Single(method => method.Name == "Write" 
				// 			                && method.DeclaringType.IsGenericType 
				// 			                && method.DeclaringType.GetGenericTypeDefinition() == typeof(JsonConverter<>));
				var mi = JsonConverter_1_Write;
				mi.Invoke(converter, new object?[] { writer, value, options });
			}
			else if (converter is JsonConverterFactory factory)
			{
				throw new NotImplementedException("Factory");
			}
			else
			{
				throw new NotImplementedException($"Unknown subtype of '{typeof(JsonConverter<>).FullName}'");
			}
		}


	}

	public class ByteArrayReader : JsonConverter<byte[]>
	{
		public override bool CanConvert(Type typeToConvert) => true;
		public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetTokenAsByteArray();
		public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options) => throw new NotImplementedException();
	}
}
