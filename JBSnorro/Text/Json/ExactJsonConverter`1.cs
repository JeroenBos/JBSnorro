using System.Text.Json;
using System.Text.Json.Serialization;
using JBSnorro.Diagnostics;
using JBSnorro;
using JBSnorro.Extensions;

namespace JBSnorro.Text.Json;

public class ExactPolymorphicJsonConverter<T> : PolymorphicJsonConverter<T>, IJsonConverterIntroducer where T : class
{
	/// <summary> TKey is the type used as key in json deserialization which will trigger the attempter. </summary>
	public static JsonConverter<T> CreateAttempter<TKey>() => ExactJsonConverterAttempter<T, TKey>.Instance;

	private bool baseTypeIsIncluded => implementationTypes.ContainsKey(typeof(T));
	private readonly IReadOnlyDictionary<Type, Type> implementationTypes;
	/// <summary> This type does not need the derived types to be strictly derived. In fact, the key and values in each pair can be the same except for the base type. </summary>
	public ExactPolymorphicJsonConverter(params (Type T, Type TImpl)[] derivedTypePairs)
	   : base(derivedTypePairs.Select(t => t.T).Where(t => t != typeof(T)).ToArray())
	{
		foreach (var (t, timpl) in derivedTypePairs)
		{
			Contract.Requires(t.IsAssignableFrom(timpl), $"Implementation type '{timpl.FullName}' is not assignable to '${t.FullName}'");
			Contract.Requires(!timpl.IsAbstract(), $"Implementation type '{timpl.FullName}' is not allowed to be abstract");
			if (t == typeof(T))
				Contract.Requires(timpl != typeof(T), $"If the key type '{t.FullName}' is the generic type parameter, then the implementation type must be different.");
		}

		this.implementationTypes = derivedTypePairs.ToReadOnlyDictionary(keySelector: t => t.T, valueSelector: t => t.TImpl);
	}
	public IEnumerable<JsonConverter> IntroducedConverters
	{
		get => this.implementationTypes
				   .Select(p => p.Key == typeof(T) ? (p.Value, p.Value) : (p.Key, p.Value))
				   .Select(ExactJsonConverterExtensions.GetOrCreate);
	}

	protected override bool Read(Utf8JsonReader reader, JsonSerializerOptions options, out T? value)
	{
		if (base.Read(reader, options, out value))
			return true;

		if (this.baseTypeIsIncluded)
		{
			try
			{
				value = ReadDerived(reader, typeof(T), options);
				return true;
			}
			catch (JsonException)
			{
				return false;
			}
		}

		return false;
	}
	protected override T ReadDerived(Utf8JsonReader reader, Type derivedType, JsonSerializerOptions options)
	{
		var implementationType = this.implementationTypes[derivedType]!;

		// we could call converter.Read directly, but we allow for other hooks. 
		// Besides, converter is here for debugging purposes only
		return (T?)JsonSerializer.Deserialize(ref reader, implementationType, options)!;
	}
}

public static class ExactJsonConverterExtensions
{
	internal static JsonConverter GetOrCreate(KeyValuePair<Type, Type> pair) => GetOrCreate(pair.Key, pair.Value);
	internal static JsonConverter GetOrCreate((Type, Type) pair) => GetOrCreate(pair.Item1, pair.Item2);
	/// <summary> Creates an ExactJsonConverter{t, timpl} </summary> where t : T where timpl : T
	public static JsonConverter GetOrCreate(Type t, Type timpl)
	{
		Contract.Requires(t.IsAssignableFrom(timpl));

		var exactJsonConverterType = typeof(ExactJsonConverter<,>).MakeGenericType(new Type[] { t, timpl });
		// var result = exactJsonConverterType.InvokeMember(nameof(ExactJsonConverter<T, T>.Create), BindingFlags.Static | BindingFlags.Public, null, null, Array.Empty<object>());
		var result = exactJsonConverterType.GetProperty(nameof(ExactJsonConverter<object, object>.Instance))!.GetValue(null);
		return (JsonConverter)(result ?? throw new ContractException());
	}
}
public class ExactJsonConverter<T, TImpl> : JsonConverter<T>
	where TImpl : T  // TImpl : T is a strategy to prevent infinite loops in the JsonDeserializer
{
	public static JsonConverter<T> Instance { get; } = Create();

	static JsonConverter<T> Create()
	{
		var expectedProperties = typeof(TImpl).GetFlattenedProperties()
											  .Where(p => !p.HasAttribute<JsonExtensionDataAttribute>())
											  .Select(p => p.Name)
											  .ToArray();
		return new ExactJsonConverter<T, TImpl>(expectedProperties);
	}

	private readonly string[] expectedProperties;
	private ExactJsonConverter(string[] expectedProperties)
	{
		this.expectedProperties = expectedProperties;
	}

	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (typeToConvert != typeof(T))
			throw new ArgumentException("Converting wrong type", nameof(typeToConvert));

		using var _ = this.DetectStackoverflow(reader, typeToConvert);

		reader.AssertHasExactlyTheseProperties(this.expectedProperties, options);

		return JsonSerializer.Deserialize<TImpl>(ref reader, options);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		static TImpl ToImpl(T t)
		{
			throw new NotImplementedException();
		}

		var impl = ToImpl(value);
		JsonSerializer.Serialize<T>(writer, impl, options);
	}
}

/// <summary> Attempts to do a json deserialization on any token, tries to read a T. </summary>
class ExactJsonConverterAttempter<T, TKey> : JsonConverter<T> where T : class
{
	public static JsonConverter<T> Instance { get; } = Create();
	private static JsonConverter<T> Create()
	{
		return new ExactPolymorphicJsonConverter<T>((typeof(TKey), typeof(T)));
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return typeof(TKey).IsAssignableFrom(typeToConvert);
	}

	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return Instance.Read(ref reader, typeof(object), options);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		Instance.Write(writer, value, options);
	}
}
