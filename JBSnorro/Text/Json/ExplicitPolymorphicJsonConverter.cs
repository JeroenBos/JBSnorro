using System.Text.Json;
using System.Text.Json.Serialization;
using JBSnorro.Diagnostics;

namespace JBSnorro.Text.Json;

/// <summary>
/// Deserializes objects polymorphically by using a property that serves as key to identify the actual
/// type of the deserialized object, and delegates to the corresponding deserializer.
/// </summary>
public class ExplicitPolymorphicJsonConverter<T, TKey> : JsonConverter<T>
{
    private readonly string keyPropertyName;
    private readonly Func<TKey, Type> getTypeToDeserialize;
    private readonly IEqualityComparer<string> keyPropertyNameEqualityComparer;
    private readonly Func<T, Either<Type, JsonConverter<T>>>? getSerializerOrTypeKey;
    /// <param name="getSerializerOrTypeKey"> This type but this allows you to delegate to another converter resolve it via another key type. </param>
    public ExplicitPolymorphicJsonConverter(string keyPropertyName,
                                            Func<TKey, Type> getTypeKeyToDeserialize,
                                            Func<T, Either<Type, JsonConverter<T>>>? getSerializerOrTypeKey = null,
                                            IEqualityComparer<string>? keyPropertyNameEqualityComparer = null)
    {
        Contract.Requires(keyPropertyName != null);
        Contract.Requires(getTypeKeyToDeserialize != null);

        this.keyPropertyName = keyPropertyName;
        this.getTypeToDeserialize = getTypeKeyToDeserialize;
        this.getSerializerOrTypeKey = getSerializerOrTypeKey;
        this.keyPropertyNameEqualityComparer = keyPropertyNameEqualityComparer ?? EqualityComparer<string>.Default;
    }

    public override T? Read(ref Utf8JsonReader _reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var _ = this.DetectStackoverflow(_reader, typeToConvert);

        Utf8JsonReader reader = _reader;
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object to get property of for explicit type mapping");

        reader.Read();
        while (reader.TokenType == JsonTokenType.PropertyName)
        {
            string? keyName = reader.GetString();
            if (keyPropertyNameEqualityComparer.Equals(keyName, this.keyPropertyName))
            {
                reader.Read();
                TKey? keyValue = JsonSerializer.Deserialize<TKey>(ref reader, options);
                Contract.Assert<JsonException>(keyValue is not null, "key must not be null");

                Type type = this.getTypeToDeserialize(keyValue);
                if (type == typeof(T))
                {
                    throw new ContractException($"{nameof(getTypeToDeserialize)} may not return 'T' (={typeof(T).FullName})");
                }

                object? result = JsonSerializer.Deserialize(ref _reader /*continue with original reader*/, type, options);
                return (T?)result;
            }
            else
            {
                SkipProperty(ref reader);
            }
        }
        throw new JsonException($"Key '{this.keyPropertyName}' not found");
    }
    static void SkipProperty(ref Utf8JsonReader r)
    {
        r.Read();
        r.GetTokenAsJson(); // just propagate the reader: not checking anything
        r.Read(); // read the endobject/endarray of the previous property
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (getSerializerOrTypeKey == null)
            throw new NotSupportedException($"Serialization not supported as no '{nameof(getSerializerOrTypeKey)}' has been provided in the constructor");

        var either = getSerializerOrTypeKey(value);
        if (either.Get(out Type key, out JsonConverter<T> converter))
        {
            Contract.Requires(key != typeof(T), $"{nameof(getSerializerOrTypeKey)} must return not return this converter's key");
            using var _ = this.DetectStackoverflow(writer, typeof(T));
            JsonSerializer.Serialize(writer, value, key, options);
        }
        else
        {
            Contract.Requires(!ReferenceEquals(converter, this), $"{nameof(getSerializerOrTypeKey)} must return not return this converter");
            converter.Write(writer, value, options);
        }
    }
}
