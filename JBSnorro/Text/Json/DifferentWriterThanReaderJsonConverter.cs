#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json
{
	public class DifferentWriterThanReaderJsonConverter<T> : JsonConverter<T>
	{
		private static T defaultReader(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return (T)DefaultObjectJsonConverter.Read(ref reader, typeToConvert, options)!;
		}
		private static void defaultWriter(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			DefaultObjectJsonConverter.Write(writer, value, options);  // not sure about the last argument
		}

		private readonly JsonConverter<T>? reader;
		private readonly JsonConverter<T>? writer;

		private DifferentWriterThanReaderJsonConverter(JsonConverter<T>? reader, JsonConverter<T>? writer) => (this.reader, this.writer) = (reader, writer);

		/// <param name="reader">null for default. </param>
		/// <param name="writer">null for default. </param>
		public static DifferentWriterThanReaderJsonConverter<T> Create(JsonConverter<T>? reader, JsonConverter<T>? writer)
		{
			if (reader == null && writer == null)
				throw new ArgumentException("Either a reader or writer must be specified");

			// simplication/optimization:
			if (reader is DifferentWriterThanReaderJsonConverter<T> r)
				reader = r.reader;
			if (writer is DifferentWriterThanReaderJsonConverter<T> w)
				writer = w.writer;

			return new DifferentWriterThanReaderJsonConverter<T>(reader, writer);
		}
		public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (this.reader != null)
				return this.reader.Read(ref reader, typeToConvert, options);
			else
				return defaultReader(ref reader, typeToConvert, options);
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			if (this.writer != null)
				this.writer.Write(writer, value, options);
			else
				defaultWriter(writer, value, options);
		}
	}
	public static class DifferentWriterThanReaderJsonConverterExtensions
	{
		/// <param name="reader">null for default. </param>
		public static JsonConverter<T> WithReader<T>(this JsonConverter<T> writer, JsonConverter<T>? reader)
		{
			return DifferentWriterThanReaderJsonConverter<T>.Create(reader, writer);
		}
		/// <param name="writer">null for default. </param>
		public static JsonConverter<T> WithWriter<T>(this JsonConverter<T> reader, JsonConverter<T>? writer)
		{
			return DifferentWriterThanReaderJsonConverter<T>.Create(reader, writer);
		}
	}
}
