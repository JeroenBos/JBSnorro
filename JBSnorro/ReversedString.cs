using JBSnorro.Algorithms;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JBSnorro
{
	public sealed class ReversedString : ICountable<char>, IIndexable<char>, IEquatable<ReversedString>, IEquatable<string>, IComparable<ReversedString>, IComparable<string>, IComparable<IEnumerable<char>>
	{
		/// <summary> Gets an empty reversed string. </summary>
		public static readonly ReversedString Empty = new ReversedString("");

		/// <summary> The reversed string represented by this instance. </summary>
		public string Value { get; private set; }
		/// <summary> The unreversed string represented by this instance. </summary>
		public string Original { get; private set; }
		/// <summary> Gets the number of characters in the reversed string. </summary>
		public int Length
		{
			get { return Value.Length; }
		}
		/// <summary> Gets the index in this reversed string at the specified index. </summary>
		/// <param name="index"> The index of the character to get. </param>
		public char this[int index]
		{
			get { return Value[index]; }
		}

		/// <summary> Creates a new reversed string containing the specified characters. </summary>
		/// <param name="original"> The characters to represent, that aren't reversed yet. </param>
		[DebuggerHidden]
		public ReversedString(IEnumerable<char> original)
		{
			Contract.Requires(original != null);

			this.Original = string.Concat(original);
			this.Value = this.Original.Reverse();
		}
		/// <summary> Creates a new reversed string containing the specified string. </summary>
		/// <param name="original"> The not reversed string to represent. </param>
		[DebuggerHidden]
		public ReversedString(string original)
		{
			Contract.Requires(original != null);

			this.Original = original;
			this.Value = original.Reverse();
		}

		/// <summary> Compares the specified characters with this reversed string. </summary>
		/// <param name="end"> The characters to compare to, as in, the beginning of the reversed string. </param>
		public int CompareTo(IEnumerable<char>? end)
		{
			if (end == null) throw new NotImplementedException();
			return StringExtensions.CompareTo(Value, end);
		}

		#region Trivial members
		public IEnumerator<char> GetEnumerator()
		{
			return Value.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		int ICountable<char>.Count
		{
			get { return Value.Length; }
		}

		public static bool operator ==(ReversedString a, ReversedString b)
		{
			if (ReferenceEquals(a, null))
				return ReferenceEquals(b, null);
			return a.Value == b;
		}
		public static bool operator !=(ReversedString a, ReversedString b)
		{
			return !(a == b);
		}
		public static bool operator ==(string a, ReversedString b)
		{
			if (ReferenceEquals(a, null))
				return ReferenceEquals(b, null);
			if (ReferenceEquals(b, null))
				return false;

			return a == b.Value;
		}
		public static bool operator !=(string a, ReversedString b)
		{
			return !(a == b);
		}
		public static bool operator ==(ReversedString a, string b)
		{
			return b == a;
		}
		public static bool operator !=(ReversedString a, string b)
		{
			return !(a == b);
		}
		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ReversedString && Equals((ReversedString)obj);
		}
		public bool Equals(string? other)
		{
			return !ReferenceEquals(other, null) && other == this.Value;
		}
		public bool Equals(ReversedString? other)
		{
			return !ReferenceEquals(other, null) && other.Value == this.Value;
		}
		public override int GetHashCode()
		{
			return (Value != null ? Value.GetHashCode() : 0);
		}

		public int CompareTo(ReversedString? other)
		{
			if (ReferenceEquals(other, null))
				return 1;
			return this.Value.CompareTo(other.Value);
		}

		public int CompareTo(string? other)
		{
			if (ReferenceEquals(other, null))
				return 1;
			return this.Value.CompareTo(other);
		}

		public override string ToString()
		{
			return Value;
		}

		#endregion
	}

	public static class ReversedStringExtensions
	{
		/// <summary> Finds a reversed string in the values of a dictionary that equals the specified characters. </summary>
		/// <typeparam name="TKey"> The type of the key in the dictionary. </typeparam>
		/// <param name="dictionary"> The dictionary to find the string in. </param>
		/// <param name="end"> The characters to find in a reversed string. The end of the string, and thus, the beginning of the reversed string. </param>
		/// <returns> a reversed string if it matches the specified characters, or null, if there is no such string in the dictionary values. </returns>
		public static ReversedString Find<TKey>(this DictionarySortedValues<TKey, ReversedString> dictionary, IEnumerable<char> end)
		{
			Contract.Requires(dictionary != null);
			Contract.Requires(end != null);

			return dictionary.Values.FirstOrDefault(end, (s, c) => StringExtensions.CompareTo(s.Value, c.Take(s.Length)));
		}
	}
}
