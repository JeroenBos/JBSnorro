#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JBSnorro.Text.Json
{
	/// <summary> Intended usage: At the top of an JsonConverter.Read override, write:
	/// `using var _ = this.DetectStackoverflow(reader, typeToConvert);`. 
	/// Which will throw in case the exact same arguments have been applied. 
	///
	/// IMPORTANT: Only do so if no other base type so already.
	/// </summary>
	public class StackoverflowDetector : IDisposable
	{
		[ThreadStatic]
		private static List<StackoverflowDetector>? _readerStack;
		[ThreadStatic]
		private static List<StackoverflowDetector>? _writerStack;
		private static List<StackoverflowDetector> writerStack
		{
			get
			{
				if (_writerStack == null)
					_writerStack = new List<StackoverflowDetector>();
				return _writerStack;
			}
		}
		private static List<StackoverflowDetector> readerStack
		{
			get
			{
				if (_readerStack == null)
					_readerStack = new List<StackoverflowDetector>();
				return _readerStack;
			}
		}

		public static bool IsOnStack(StackoverflowDetector item)
		{
			return item.stack.Any<StackoverflowDetector>(item.Equals);
		}

		private readonly JsonConverter converter;
		private readonly SequencePosition readerPosition;
		private readonly Type typeToConvert;
		private readonly long writerPosition;
		private List<StackoverflowDetector> stack => this.writerPosition == -1 ? readerStack : writerStack;

		public StackoverflowDetector(JsonConverter converter, Utf8JsonReader reader, Type typeToConvert)
		{
			this.converter = converter;
			this.typeToConvert = typeToConvert;
			this.writerPosition = -1;
			this.readerPosition = reader.Position;

			if (IsOnStack(this))
				throw new StackOverflowException();
			else
				this.stack.Add(this);
		}
		public StackoverflowDetector(JsonConverter converter, Utf8JsonWriter writer, Type typeToConvert)
		{
			this.converter = converter;
			this.typeToConvert = typeToConvert;
			this.writerPosition = writer.BytesCommitted + writer.BytesPending;

			if (IsOnStack(this))
				throw new StackOverflowException();
			else
				this.stack.Add(this);
		}


		public override bool Equals(object? obj) => Equals(obj as StackoverflowDetector);

		public bool Equals(StackoverflowDetector? obj)
		{
			if (ReferenceEquals(obj, null))
				return false;

			return obj.converter?.GetType() == this.converter?.GetType()
				&& obj.typeToConvert == this.typeToConvert
				&& obj.readerPosition.Equals(this.readerPosition);
		}
		public override int GetHashCode() => throw new InvalidOperationException();

		public void Dispose()
		{
			this.stack.Remove(this);
		}
	}
	public static class StackoverflowDetectorExtensions
	{
		public static StackoverflowDetector DetectStackoverflow(this JsonConverter converter, Utf8JsonReader reader, Type typeToConvert)
		{
			return new StackoverflowDetector(converter, reader, typeToConvert);
		}
		public static StackoverflowDetector DetectStackoverflow(this JsonConverter converter, Utf8JsonWriter writer, Type typeToConvert)
		{
			return new StackoverflowDetector(converter, writer, typeToConvert);
		}
	}
}
