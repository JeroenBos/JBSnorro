using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json;


/// <summary> Default version of DictionaryJsonConverter`2 with default TSubsitute. </summary>
public class DictionaryJsonConverter<TValue> : DictionaryJsonConverter<TValue, IReadOnlyDictionary<string, TValue>>
{

	public DictionaryJsonConverter(IReadOnlyDictionary<string, Type?>? elementTypes = null)
		: base(d => new ReadOnlyDictionary<string, TValue>(d), elementTypes)
	{
	}
}

/// <summary> A dictionary deserializer the also deserialized the elements. 
/// And per element the typeToConvert can be specified (and will continue to use the one and only JsonSerializationOptions). </summary>
public class DictionaryJsonConverter<TValue, TSubstitute> : JsonConverter<TSubstitute>
	where TSubstitute : IReadOnlyDictionary<string, TValue>
{
	private static readonly IReadOnlyDictionary<string, Type?> emptyDictionary = new Dictionary<string, Type?>();
	private static readonly JsonTokenType[] _primitiveTokenTypes = new JsonTokenType[] { JsonTokenType.Null, JsonTokenType.False, JsonTokenType.Number, JsonTokenType.String, JsonTokenType.True };
	static DictionaryJsonConverter()
	{
		if (typeof(TSubstitute) == typeof(Dictionary<string, TValue>))
			throw new ArgumentException("The type argument 'Dictionary<string, TValue>' is going to result in a stackoverflow. ", nameof(TSubstitute));
	}


	private readonly Func<Dictionary<string, TValue>, TSubstitute> ctor;
	private readonly IReadOnlyDictionary<string, Type?> elementTypes;
	/// <param name="elementTypes"> Can specify per key which type is to be used for `typeToConvert` in json deserialization. </param>
	public DictionaryJsonConverter(Func<Dictionary<string, TValue>, TSubstitute> ctor,
								   IReadOnlyDictionary<string, Type?>? elementTypes = null)
	{
		Contract.Requires(ctor != null);
		this.ctor = ctor;
		this.elementTypes = elementTypes ?? emptyDictionary;
	}

	public override TSubstitute Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var _ = this.DetectStackoverflow(reader, typeToConvert);
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException("Not a dictionary");
		}

		var result = new Dictionary<string, TValue>();
		while (true)
		{
			reader.Read(); // StartObject
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				break;
			}
			else if (reader.TokenType != JsonTokenType.PropertyName)
			{
				throw new JsonException("Not a dictionary");
			}
			else
			{
				var name = reader.GetString();
				if (name == null) throw new ContractException();

				reader.Read();


				Type? typeKey = this.elementTypes.GetValueOrDefault(name);
				if (typeKey == null)
				{
					if (TryReadPrimitiveType(ref reader, out TValue value, options))
					{
						result[name] = value;
						continue;
					}
					else if (reader.TokenType != JsonTokenType.StartArray && reader.TokenType != JsonTokenType.StartObject)
					{
					}
				}

				var deserialized = JsonSerializer.Deserialize(ref reader, typeKey ?? typeof(TValue), options);
				try
				{
					result[name] = (TValue)(object?)deserialized!;
				}
				catch (InvalidCastException e)
				{
					throw new JsonException($"Failed to deserialize value for key '{name}'", e);
				}
			}
		}
		return ctor(result);
	}
	public override void Write(Utf8JsonWriter writer, TSubstitute value, JsonSerializerOptions options)
	{
		var dictionary = value as Dictionary<string, TValue>
					?? value.ToDictionary(keySelector: kvp => kvp.Key, elementSelector: kvp => kvp.Value);
		JsonSerializer.Serialize<Dictionary<string, TValue>?>(writer, dictionary, options);
	}
	static bool TryReadPrimitiveType(ref Utf8JsonReader reader, out TValue value, JsonSerializerOptions options)
	{
		if (reader.TokenType.IsAnyOf(_primitiveTokenTypes))
		{
			var _json = JsonSerializer.Deserialize<object>(ref reader, options);
			if (_json is JsonElement json)
			{
				// unpack JsonElement if it's a primitive:
				value = ReadPrimitiveType(json);
				return true;
			}
			else
			{
				throw new NotImplementedException("How?");
			}
		}
		else if (reader.TokenType == JsonTokenType.StartArray)
		{
			if (typeof(TValue).IsArray || typeof(TValue) == typeof(object))
			{
				var copy = reader;
				copy.Read();
				if (copy.TokenType == JsonTokenType.EndArray)
				{
					value = (TValue)(object)Array.Empty<object>(); // by array variance this will never throw
					reader.Read(); // get reader up to speed
					return true;
				}
			}
		}
		value = default!;
		return false;
	}
	static TValue ReadPrimitiveType(JsonElement json)
	{
		switch (json.ValueKind)
		{
			case JsonValueKind.String:
				if (typeof(TValue) == typeof(object) || typeof(TValue) == typeof(string))
				{
					string? s = json.GetString();
					if (s == null) throw new ContractException();

					return (TValue)(object)s;
				}
				else
					throw new JsonException($"Converting JsonValueKind.String to '{typeof(TValue).Name}' is not implemented");
			case JsonValueKind.Number:
				// for now we just default to a float:
				if (typeof(TValue) == typeof(object) || typeof(TValue) == typeof(float))
				{
					float f = json.GetSingle();
					return (TValue)(object)f;
				}
				else
					throw new JsonException($"Converting JsonValueKind.Number to '{typeof(TValue).Name}' is not implemented");
			case JsonValueKind.True:
			case JsonValueKind.False:
				if (typeof(TValue) == typeof(object) || typeof(TValue) == typeof(bool))
				{
					bool b = json.GetBoolean();
					return (TValue)(object)b;
				}
				else
					throw new JsonException($"Converting JsonValueKind.True/False to '{typeof(TValue).Name}' is not implemented");
			case JsonValueKind.Null:
				return default(TValue)!;
			default:
				throw new ArgumentException();
		}
	}

}
