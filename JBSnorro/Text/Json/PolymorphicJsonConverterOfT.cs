using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json
{

	/// <summary>
	/// The default <see cref="JsonConverter"/> does not serialize any fields or properties of runtime subtypes. 
	/// This converter writes an object of runtime type TRuntime : T as if it has compile time type TRuntime, whereas by default it is T.
	/// </summary>
	/// <typeparam name="T"> The compile-time type of the object to serialize. </typeparam>
	/// <see href="https://github.com/dotnet/runtime/blob/master/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/JsonConverterOfT.cs"/>
	public class PolymorphicJsonWriter<T> : JsonConverter<T> where T : class
	{
		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			Contract.Requires(writer != null);
			Contract.Requires(value == null || value.GetType() != typeof(T), "Infinite loop created. Preferably T in PolymorphicJsonConverter<T> is abstract");
			Contract.Requires(value != null);
			Contract.Requires(options != null);

			JsonSerializer.Serialize(writer, value, value.GetType(), options);
		}
		public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
	}

	public class PolymorphicJsonConverter<T> : PolymorphicJsonWriter<T> where T : class
	{
		private readonly Type[] derivedTypes;

		public static PolymorphicJsonConverter<T> Create(params Type[] derivedTypes)
		{
			return new PolymorphicJsonConverter<T>(derivedTypes);
		}
		protected PolymorphicJsonConverter(Type[] derivedTypes)
		{
			Contract.Requires(derivedTypes != null);
			Contract.RequiresForAll(derivedTypes, type => type != null, $"Types must not be null");
			Contract.RequiresForAll(derivedTypes, typeof(T).IsAssignableFrom, $"Types must be subtypes of type parameter {nameof(T)} (index = {{0}})");
			Contract.RequiresForAll(derivedTypes, type => type != typeof(T), $"Types must be strict subtypes of type parameter {nameof(T)} (index = {{0}})");
			// Contract.RequiresForAll(derivedTypes, type => type.IsClass, $"Types must be classes (index = {{0}})");
			// Contract.RequiresForAll(derivedTypes, type => !type.IsAbstract, $"Types must not be abstract (index = {{0}})");
			Contract.Requires(derivedTypes.AreUnique(), "The types must be unique");
			// Contract.RequiresForAll(derivedTypes, type => type.GetConstructor(Array.Empty<Type>()) != null, $"Types must have a parameterless ctor (index = {{0}})");
			foreach (var (derivedType, i) in derivedTypes.WithIndex())
			{
				int j = derivedTypes.Take(i).IndexOf((earlierType, j) => earlierType.IsAssignableFrom(derivedType));
				Contract.Requires(j == -1, j == -1 ? "" : $"Reevaluate the order. Type '{derivedTypes[i].FullName}' at index {i} is assignable to type '{derivedTypes[j].FullName}' at index {j} and will never be considered");
			}

			this.derivedTypes = derivedTypes;
		}

		public sealed override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			Contract.Requires(typeToConvert == typeof(T));

			// assuming Utf8JsonReader is truly immutable, copying it and working with the copy should not affect the original reader
			// so here we propagate the original reader once, and then only work with copies
			Utf8JsonReader cachedReader = reader.GetTokenAsJsonReader();

			if (this.Read(cachedReader, options, out T? result))
				return result;

			string debug = cachedReader.GetTokenAsJson();

			throw new JsonException($"Couldn't convert json to type '{typeToConvert.FullName}'");
		}
		protected virtual bool Read(Utf8JsonReader reader, JsonSerializerOptions options, out T? value)
		{
			foreach (var type in this.derivedTypes)
			{
				object? deserialized;
				try
				{
					// reader is copied
					deserialized = this.ReadDerived(reader, type, options);
				}
				catch (JsonException)
				{
					continue; // try next type
				}
#if DEBUG
				if (!(deserialized is T))
				{
					var succeededDeserializations = new List<Type>();

					foreach (var t in this.derivedTypes.SkipWhile(t => t != type).Skip(1))
					{
						try
						{
							// reader is copied
							deserialized = this.ReadDerived(reader, t, options);
						}
						catch (JsonException)
						{
							continue;
						}
						succeededDeserializations.Add(t);
					}
					string message = $"Couldn't convert from type '{deserialized?.GetType().FullName ?? "<null>"}' to '{typeof(T).FullName}'. ";
					if (succeededDeserializations.Count == 0)
						message += "There were no other types to which the json could be deserialized. ";
					else
					{
						message += "Consider reevaluating the order of the subtypes. The other types to which the json could be deserialized ";
						message += succeededDeserializations.Count == 1 ? "is: " : "are: ";
						message += succeededDeserializations.Select(t => t.FullName).Select(name => $"'{name}'").Join(", ");
					}
					throw new InvalidCastException(message);
				}
#endif
				value = (T?)deserialized;
				return true;
			}
			value = default;
			return false;
		}
		protected virtual T? ReadDerived(Utf8JsonReader reader, Type derivedType, JsonSerializerOptions options)
		{
			return (T?)JsonSerializer.Deserialize(ref reader, derivedType, options);
		}
	}
	public class MappedJsonConverter<TDeserialized, TSerialized> : JsonConverter<TDeserialized>
	{
		public static JsonConverter<TDeserialized> Create(Func<TSerialized, TDeserialized> map, Func<TDeserialized, TSerialized>? inverseMap = null)
		{
			Contract.Requires(map != null);
			return new MappedJsonConverter<TDeserialized, TSerialized>(map, inverseMap);
		}

		private readonly Func<TSerialized, TDeserialized> map;
		private readonly Func<TDeserialized, TSerialized>? inverseMap;
		protected MappedJsonConverter(Func<TSerialized, TDeserialized> map, Func<TDeserialized, TSerialized>? inverseMap) => (this.map, this.inverseMap) = (map, inverseMap);
		public override TDeserialized Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using var _ = this.DetectStackoverflow(reader, typeToConvert);
			
			Contract.Requires(typeToConvert == typeof(TDeserialized));
			var obj = JsonSerializer.Deserialize<TSerialized>(ref reader, options)!;
			var result = map(obj);
			return result;
		}

		public override void Write(Utf8JsonWriter writer, TDeserialized value, JsonSerializerOptions options)
		{
			Contract.Requires<NotSupportedException>(inverseMap != null, $"No {nameof(inverseMap)} provided");

			var inverted = inverseMap(value);
			JsonSerializer.Serialize(inverted, options);
		}
	}

}
