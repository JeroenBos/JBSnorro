using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json;

public abstract class ExplicitJsonConverter<T> : JsonConverter<T>
{
	public static ExplicitJsonConverter<T> Create(Type constantsType)
	{
		return Create(_ => constantsType);
	}
	public static ExplicitJsonConverter<T> Create(Func<Type, Type> getType)
	{
		return new ExplicitJsonConverterImpl<T>(getType);
	}
	protected abstract Type GetType(Type typeToConvert, JsonSerializerOptions options);
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var type = this.GetType(typeToConvert, options);
		return (T?)DefaultObjectJsonConverter.Read(ref reader, type, options);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		DefaultObjectJsonConverter.Write(writer, value, options);
	}
}
class ExplicitJsonConverterImpl<T> : ExplicitJsonConverter<T>
{
	public ExplicitJsonConverterImpl(Func<Type, Type> getType)
	{
		this.getType = getType;
	}
	private readonly Func<Type, Type> getType;
	protected override Type GetType(Type typeToConvert, JsonSerializerOptions options)
	{
		return getType(typeToConvert);
	}
}
