using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace JBSnorro.Text
{
	public class StringBuilderAdapter : IStringBuilder
	{
		private readonly StringBuilder _stringBuilder;
		public StringBuilderAdapter(StringBuilder stringBuilder)
		{
			Contract.Requires(stringBuilder != null);
			this._stringBuilder = stringBuilder;
		}

		public virtual char this[int index] { get => _stringBuilder[index]; set => _stringBuilder[index] = value; }

		public virtual int Capacity { get => _stringBuilder.Capacity; set => _stringBuilder.Capacity = value; }
		public virtual int Length { get => _stringBuilder.Length; set => _stringBuilder.Length = value; }

		public virtual int MaxCapacity => _stringBuilder.MaxCapacity;

		public virtual StringBuilder Append(char value, int repeatCount)
		{
			return _stringBuilder.Append(value, repeatCount);
		}

		public virtual StringBuilder Append(bool value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(char value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(ulong value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(uint value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(byte value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(string value, int startIndex, int count)
		{
			return _stringBuilder.Append(value, startIndex, count);
		}

		public virtual StringBuilder Append(string value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(float value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(ushort value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(object value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(char[] value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(char[] value, int startIndex, int charCount)
		{
			return _stringBuilder.Append(value, startIndex, charCount);
		}

		public virtual StringBuilder Append(sbyte value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(decimal value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(short value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(int value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(long value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder Append(double value)
		{
			return _stringBuilder.Append(value);
		}

		public virtual StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0)
		{
			return _stringBuilder.AppendFormat(provider, format, arg0);
		}

		public virtual StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0, object arg1)
		{
			return _stringBuilder.AppendFormat(provider, format, arg0, arg1);
		}

		public virtual StringBuilder AppendFormat(IFormatProvider provider, string format, params object[] args)
		{
			return _stringBuilder.AppendFormat(provider, format, args);
		}

		public virtual StringBuilder AppendFormat(string format, object arg0)
		{
			return _stringBuilder.AppendFormat(format, arg0);
		}

		public virtual StringBuilder AppendFormat(string format, object arg0, object arg1)
		{
			return _stringBuilder.AppendFormat(format, arg0, arg1);
		}

		public virtual StringBuilder AppendFormat(string format, object arg0, object arg1, object arg2)
		{
			return _stringBuilder.AppendFormat(format, arg0, arg1, arg2);
		}

		public virtual StringBuilder AppendFormat(string format, params object[] args)
		{
			return _stringBuilder.AppendFormat(format, args);
		}

		public virtual StringBuilder AppendFormat(IFormatProvider provider, string format, object arg0, object arg1, object arg2)
		{
			return _stringBuilder.AppendFormat(provider, format, arg0, arg1, arg2);
		}

		public virtual StringBuilder AppendLine()
		{
			return _stringBuilder.AppendLine();
		}

		public virtual StringBuilder AppendLine(string value)
		{
			return _stringBuilder.AppendLine(value);
		}

		public virtual StringBuilder Clear()
		{
			return _stringBuilder.Clear();
		}

		public virtual void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
		{
			_stringBuilder.CopyTo(sourceIndex, destination, destinationIndex, count);
		}

		public virtual int EnsureCapacity(int capacity)
		{
			return _stringBuilder.EnsureCapacity(capacity);
		}

		public virtual bool Equals(StringBuilder sb)
		{
			return _stringBuilder.Equals(sb);
		}

		public virtual StringBuilder Insert(int index, char[] value, int startIndex, int charCount)
		{
			return _stringBuilder.Insert(index, value, startIndex, charCount);
		}

		public virtual StringBuilder Insert(int index, bool value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, byte value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, ulong value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, char[] value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, ushort value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, string value, int count)
		{
			return _stringBuilder.Insert(index, value, count);
		}

		public virtual StringBuilder Insert(int index, char value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, uint value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, sbyte value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, object value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, long value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, int value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, short value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, double value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, decimal value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, float value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Insert(int index, string value)
		{
			return _stringBuilder.Insert(index, value);
		}

		public virtual StringBuilder Remove(int startIndex, int length)
		{
			return _stringBuilder.Remove(startIndex, length);
		}

		public virtual StringBuilder Replace(char oldChar, char newChar)
		{
			return _stringBuilder.Replace(oldChar, newChar);
		}

		public virtual StringBuilder Replace(char oldChar, char newChar, int startIndex, int count)
		{
			return _stringBuilder.Replace(oldChar, newChar, startIndex, count);
		}

		public virtual StringBuilder Replace(string oldValue, string newValue)
		{
			return _stringBuilder.Replace(oldValue, newValue);
		}

		public virtual StringBuilder Replace(string oldValue, string newValue, int startIndex, int count)
		{
			return _stringBuilder.Replace(oldValue, newValue, startIndex, count);
		}

		public virtual string ToString(int startIndex, int length)
		{
			return _stringBuilder.ToString(startIndex, length);
		}

		public override string ToString()
		{
			return this._stringBuilder.ToString();
		}
	}
}
