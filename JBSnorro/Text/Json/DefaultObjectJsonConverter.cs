using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;

namespace JBSnorro.Text.Json;

/// <summary> Imitates the behavior of <see cref="JsonSerializer"/> as if there was a default <see cref="JsonConverter"/> for object. </summary>
public sealed class DefaultObjectJsonConverter : DefaultJsonConverter<object?>
{
	public static void Write(Utf8JsonWriter writer,
							 object? value,
							 JsonSerializerOptions nestedOptions,
							 Type? nestedInputType = null)
	{
		JsonConverter<object?> converter = new DefaultObjectJsonConverter(nestedOptions, nestedInputType);
		converter.Write(writer, value, nestedOptions);
	}
	public static object? Read(ref Utf8JsonReader reader,
							   Type typeToConvert,
							   JsonSerializerOptions nestedOptions,
							   Func<Type, JsonSerializerOptions, IEnumerable<PropertyInfo>>? getPropertiesToRead = null)
	{
		getPropertiesToRead ??= GetDefaultPropertiesToRead;
		switch (reader.TokenType)
		{
			case JsonTokenType.StartObject:
				{
					var obj = Activator.CreateInstance(typeToConvert);
					var properties = getPropertiesToRead(typeToConvert, nestedOptions)
										  .ToDictionary(keySelector: p => p.Name.ToLower(), StringComparer.CurrentCultureIgnoreCase);
					var extensionProperties = getPropertiesToRead(typeToConvert, nestedOptions).Where(p => p.HasAttribute<JsonExtensionDataAttribute>())
																							   .Take(2)
																							   .ToList();
					PropertyInfo? extensionProperty = null;
					if (extensionProperties.Count == 2)
						throw new InvalidOperationException("Multiple properties decorated with 'JsonExtensionDataAttribute' found");
					else if (extensionProperties.Count == 1)
					{
						extensionProperty = extensionProperties[0];
						if (!typeof(IDictionary<string, object>).IsAssignableFrom(extensionProperty.PropertyType))
							throw new InvalidOperationException($"Property '{extensionProperty.Name}' decorated with 'JsonExtensionDataAttribute' is not assignable to `IDictionary<string, object>`");
					}

					IDictionary<string, object?>? extensionDictionary = null;
					if (extensionProperty != null)
					{
						extensionDictionary = new Dictionary<string, object?>();
						extensionProperty.SetValue(obj, extensionDictionary);
					}
					while (true)
					{
						reader.Read();
						if (reader.TokenType == JsonTokenType.EndObject)
							break;
						Contract.Assert(reader.TokenType == JsonTokenType.PropertyName);

						string propertyName = reader.GetString() ?? throw new ContractException();

						if (properties.TryGetValue(propertyName, out PropertyInfo? property))
						{
							var copy = reader;
							object? value;
							try
							{
								value = JsonSerializer.Deserialize(ref reader, property.PropertyType, nestedOptions);
							}
							catch (JsonException)
							{
								var token = copy.GetTokenAsJson();
								throw new JsonException($"Cannot deserialize type '{property.PropertyType}' from: \n\n" + token);
							}
							property.SetValue(obj, value);
						}
						else if (extensionDictionary != null)
						{
							var value = JsonSerializer.Deserialize<object?>(ref reader, nestedOptions);
							extensionDictionary[propertyName] = value;
						}
						else
						{
							Global.AddDebugObject("Ignoring property " + propertyName);
						}
					}
					return obj;
				}
			case JsonTokenType.StartArray:
				throw new NotImplementedException();
			case JsonTokenType.False:
			case JsonTokenType.True:
				return reader.GetBoolean();
			case JsonTokenType.Number:
				return reader.GetSingle();
			case JsonTokenType.None:
				return null;
			case JsonTokenType.String:
				return reader.GetString();
			case JsonTokenType.Comment:
				reader.Read();
				return Read(ref reader, typeToConvert, nestedOptions);
			default:
				throw new Exception("Unknown tokentype");
		}
	}

	public DefaultObjectJsonConverter(JsonSerializerOptions? nestedOptions = null, Type? nestedInputType = null)
		: base(nestedOptions, nestedInputType)
	{
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return !typeof(IEnumerable).IsAssignableFrom(typeToConvert) 
			&& !typeToConvert.IsPrimitive 
			&& typeToConvert != typeof(string);
	}
}
/// <summary>
/// Again, the System.Text.Json package fails me. It can't covariantly serialize elements of enumerables.
/// Meaning, the following scenario I expect to work but throws an InvalidCastException instead (in .NET 6, not in .NET 3). I'm sure it's "worth" the performance. Yes I regret not choosing NetwonSoft _again_.
/// - Suppose you're serializing an array of T. 
/// - As expected, the serializer tries to find a json converter for array of T, and when it doesn't find one, uses the default enumerable converter for array or T, which is System.Text.Json.Serialization.Converters.ArrayConverter`2 (internal).
/// This ArrayConverter`2 tries to resolve a json converter for T, still completely acceptable. 
/// Suppose it finds a JsonConverter`1 that does not derive from <see cref="JsonConverter{T}" /> but e.g. <see cref="JsonConverter{U}" /> where T : U,
/// Then <see cref="JsonConverter{U}.Write(Utf8JsonWriter, U, JsonSerializerOptions)"/> would receive a T (which is perfectly valid), 
/// but the <see cref="JsonConverter{U}" /> is first cast to <see cref="JsonConverter{T}" />, which is _NOT_ valid.
/// <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Collection/IEnumerableDefaultConverter.cs"/>, in particular the GetElementConverter for the InvalidCastException.
/// 
/// Anyway, we need create a default enumerable serializer ourselves then.
/// </summary>
/// <typeparam name="TCollection"></typeparam>
public class IEnumerableJsonConverter<TCollection> : JsonConverter<TCollection> where TCollection : IEnumerable
{
	private static void WriteDictionaryElements(IDictionary dict, Type elementType, Utf8JsonWriter writer, JsonSerializerOptions options)
	{
		foreach (var key in dict.Keys)
		{
			if (key is string s)
			{
				writer.WritePropertyName(s);
			}
			else
			{
				string encodedKey = JsonSerializer.Serialize(key, key.GetType(), options);
				writer.WritePropertyName(encodedKey);
			}
			var value = dict[key];
			JsonSerializer.Serialize(writer, value, elementType, options);
		}
	}
	private static void WriteDictionaryElements<TKey, TValue>(IDictionary<TKey, TValue> dict, Utf8JsonWriter writer, JsonSerializerOptions options) where TKey : notnull
	{
		foreach (var (key, value) in dict)
		{
			if (key is string s)
			{
				writer.WritePropertyName(s);
			}
			else
			{
				string encodedKey = JsonSerializer.Serialize(key, key.GetType(), options);
				writer.WritePropertyName(encodedKey);
			}

			JsonSerializer.Serialize<TValue>(writer, value, options);
		}
	}
	private static readonly MethodInfo WriteDictionaryElementsMethodInfo = typeof(IEnumerableJsonConverter<IEnumerable>).GetMethod(nameof(WriteDictionaryElements), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)!;
	private static void ForEach(IDictionary dict, Type elementType, Utf8JsonWriter writer, JsonSerializerOptions options)
	{
		var dictionaryType = typeof(Action<,>).MakeGenericType(typeof(string), elementType);
		var delegateType = typeof(Action<,,>).MakeGenericType(dictionaryType, typeof(Utf8JsonWriter), typeof(JsonSerializerOptions));

		var forEach = Delegate.CreateDelegate(delegateType, WriteDictionaryElementsMethodInfo);
		forEach.DynamicInvoke(dict, writer, options);
	}

	/// <summary>
	/// Gets the element type for this json converter (in the case of dictionaries, that's the value type).
	/// </summary>
	private Type GetElementType(Type typeToConvert)
	{
		return getElementType(typeof(TCollection))
			?? getElementType(typeToConvert)
			?? typeof(object);

		static Type? getElementType(Type typeToConvert)
		{
			if(typeof(IDictionary<,>).IsOpenlyAssignableFrom(typeToConvert, out Type[] typeParameters))
			{
				return typeParameters[1];
			}
			if (typeof(IEnumerable<>).IsOpenlyAssignableFrom(typeToConvert, out typeParameters))
			{
				return typeParameters[0];
			}
			return null;
		}
	}
	public override TCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return (TCollection)DefaultObjectJsonConverter.Read(ref reader, typeToConvert, options)!;
	}

	public override void Write(Utf8JsonWriter writer, TCollection enumerable, JsonSerializerOptions options)
	{
		Type elementType = GetElementType(enumerable.GetType());
		Contract.Assert(!elementType.IsGenericType);

		if (enumerable is IDictionary dictionary) // typeof(IDictionary<,>).IsOpenlyAssignableFrom(enumerable.GetType()))
		{
			writer.WriteStartObject();

			WriteDictionaryElements(dictionary, elementType, writer, options);

			writer.WriteEndObject();
		}
		else if (enumerable is ICollection collection)
		{
			writer.WriteStartArray();

			foreach (var element in collection)
			{
				JsonSerializer.Serialize(writer, element, elementType, options);
			}

			writer.WriteEndArray();
		}
		else
		{
			throw new NotImplementedException("Serializing non-generic IEnumerable is not implemented yet, and the default serializer would most likely crash");
		}
	}
	public override bool CanConvert(Type typeToConvert)
	{
		return typeof(IEnumerable).IsAssignableFrom(typeToConvert) && typeToConvert != typeof(string);
	}
}
public class DefaultJsonConverter<T> : JsonConverter<T>
{
	private readonly JsonSerializerOptions nestedOptions;
	private readonly Type? nestedInputType;

	public DefaultJsonConverter(JsonSerializerOptions? nestedOptions = null, Type? childInputType = null)
	{
		this.nestedOptions = nestedOptions ?? new JsonSerializerOptions();
		this.nestedInputType = childInputType;
	}
	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		foreach (var (name, propertyValue) in GetPropertyNameAndValuesToWrite(value, nestedOptions))
		{
			Contract.Assert(name != null);

			writer.WritePropertyName(name);
			if (nestedInputType != null)
			{
				JsonSerializer.Serialize(writer, propertyValue, nestedInputType, nestedOptions);
			}
			else
			{
				JsonSerializer.Serialize(writer, propertyValue, nestedOptions);
			}
		}
		writer.WriteEndObject();
	}
	protected virtual IEnumerable<(string, object?)> GetPropertyNameAndValuesToWrite(object value, JsonSerializerOptions options)
	{
		bool hasFoundPropertyWithJsonExtensionDataAttribute = false;
		return this.GetPropertiesToWrite(value, options)
				   .SelectMany(propertyInfo =>
				   {
					   if (propertyInfo.HasAttribute<JsonExtensionDataAttribute>())
					   {
						   if (hasFoundPropertyWithJsonExtensionDataAttribute)
							   throw new InvalidOperationException("Multiple properties decorated with 'JsonExtensionDataAttribute' found");
						   else
							   hasFoundPropertyWithJsonExtensionDataAttribute = true;
						   if (!typeof(IDictionary<string, object>).IsAssignableFrom(propertyInfo.PropertyType))
							   throw new InvalidOperationException($"Property '{propertyInfo.Name}' decorated with 'JsonExtensionDataAttribute' is not assignable to `IDictionary<string, object>`");

						   var dict = (IDictionary<string, object?>?)propertyInfo.GetValue(value);
						   return dict?.Select(pair => (pair.Key, (object?)pair.Value)) ?? Enumerable.Empty<(string, object?)>();
					   }

					   return (GetPropertyName(propertyInfo, options), (object?)propertyInfo.GetValue(value)).ToSingleton();
				   })
				   .ToList();
	}
	protected virtual IEnumerable<PropertyInfo> GetPropertiesToWrite(object value, JsonSerializerOptions options)
	{
		// looking at the source code of JsonClassInfo this roughly equality the default implementation
		// except for naming conflicts with other properties. Consider that not implementing, and will silently write invalid json (containing a key twice)
		return value.GetType()
					.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
					.Where(p => p.GetMethod != null)
					.Where(p => p.GetIndexParameters().Length == 0)
					.Where(p => !p.HasAttribute<JsonIgnoreAttribute>());

		// though I don't see a difference, the actual reference source code does something more this: 
		// GetProperties(Public | NonPublic).Where(propertyInfo.GetMethod?.IsPublic == true) 
	}
	protected virtual IEnumerable<PropertyInfo> GetPropertiesToRead(Type type, JsonSerializerOptions options)
	{
		return GetDefaultPropertiesToRead(type, options);
	}
	private protected static IEnumerable<PropertyInfo> GetDefaultPropertiesToRead(Type type, JsonSerializerOptions options)
	{
		return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
				   .Where(p => p.SetMethod != null)
				   .Where(p => p.GetIndexParameters().Length == 0)
				   .Where(p => !p.HasAttribute<JsonIgnoreAttribute>());
	}
	protected virtual string GetPropertyName(PropertyInfo property, JsonSerializerOptions options)
	{
		return property.Name;
	}
	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		Contract.Requires(typeToConvert != null);
		Contract.Requires(typeof(T).IsAssignableFrom(typeToConvert), $"{nameof(typeToConvert)} '{typeToConvert.FullName}' is not convertible to T = '{typeof(T).FullName}'");
		return (T)DefaultObjectJsonConverter.Read(ref reader, typeToConvert, options, this.GetPropertiesToRead)!;
	}
}
