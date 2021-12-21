#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace JBSnorro.Text.Json
{
	internal interface IExtraPropertyJsonConverter
	{
		public static ExtraPropertyJsonConverter Create(string name, Func<object, object?> getValue, JsonSerializerOptions? jsonSerializerOptions)
		{
			return new ExtraPropertyJsonConverter(name, getValue, jsonSerializerOptions);
		}
		public static bool WillAddExtraProperty([NotNullWhen(true)] object? arg, Func<object, object?> getValue)
		{
			// this should be equivalent to:
			// reader.FirstTokenType == JsonTokenType.StartObject && this.getValue(arg) != null

			return GetExtraProperty(arg, getValue) != null;
		}

		public static object? GetExtraProperty(object? arg, Func<object, object?> getValue)
		{
			return arg?.GetType() switch
			{
				null => null,
				{ IsPrimitive: true } => null,
				{ IsArray: true } => null,
				{ IsEnum: true } => null,
				{ IsInterface: true } => null,
				{ IsSignatureType: true } => null,
				_ => getValue(arg)
			};
		}

	}
	internal class ExtraPropertyJsonConverter : DefaultJsonConverter<object>
	{
		private readonly string name;
		private readonly Func<object, object?> getValue;
		private readonly JsonSerializerOptions? nestedOptions;


		/// <param name="name"> The name of the json property that is to be added. </param>
		/// <param name="getValue"> Gets the value to be serialized as identifier of the type. If null is returned, no identifier is serialized. </param>
		/// <param name="nestedOptions"> The options to be passed when serializing everything. 
		/// This converter should be part of other options which delegates to these specified options.</param>
		public ExtraPropertyJsonConverter(string name,
										  Func<object, object?> getValue,
										  JsonSerializerOptions? nestedOptions)
			: base(nestedOptions)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException(nameof(name));
			if (getValue == null)
				throw new ArgumentNullException(nameof(getValue));

			this.name = name;
			this.getValue = getValue;
			this.nestedOptions = nestedOptions;
		}
		public bool WillAddExtraProperty([NotNullWhen(true)] object? arg)
		{
			return IExtraPropertyJsonConverter.WillAddExtraProperty(arg, this.getValue);
		}


		protected override IEnumerable<(string, object?)> GetPropertyNameAndValuesToWrite(object value, JsonSerializerOptions options)
		{
			var baseProperties = base.GetPropertyNameAndValuesToWrite(value, options);
			object? extraProperty = IExtraPropertyJsonConverter.GetExtraProperty(value, this.getValue);

			if (extraProperty == null)
				return baseProperties;
			else
				return baseProperties.Prepend((this.name, extraProperty));
		}

		public override bool CanConvert(Type typeToConvert)
		{
			return !typeof(System.Collections.IEnumerable).IsAssignableFrom(typeToConvert);
		}
	}
}
