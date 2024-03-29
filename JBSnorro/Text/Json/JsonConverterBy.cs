﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json;

// The equivalent for Newtonsoft is in JBNA
public class JsonConverterBy<T, TRepresentation> : JsonConverter<T>
{
    private Func<TRepresentation?, T?> deserialize;
    private Func<T?, TRepresentation?> serialize;

    public JsonConverterBy(Func<TRepresentation?, T?> deserialize, Func<T?, TRepresentation?> serialize)
    {
        this.deserialize = deserialize;
        this.serialize = serialize;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var intermediate = JsonSerializer.Deserialize<TRepresentation>(ref reader, options);
        var result = deserialize(intermediate);
        return result;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var intermediate = serialize(value);
        JsonSerializer.Serialize(writer, intermediate, options);
    }
}
