#nullable enable
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JBSnorro.Collections.ObjectModel
{
	/// <summary> Implements structural equality. </summary>
	public class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
	{
		protected readonly IReadOnlyDictionary<TKey, TValue> underlying;
		public ReadOnlyDictionary(IReadOnlyDictionary<TKey, TValue> underlying)
		{
			Contract.Requires(underlying != null);
			this.underlying = underlying;
		}


		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			return this.Equals(obj as IReadOnlyDictionary<TKey, TValue>);
		}
		public virtual bool Equals([NotNullWhen(true)] IReadOnlyDictionary<TKey, TValue>? attributes)
		{
			if (attributes == null)
				return false;
			if (ReferenceEquals(attributes, this))
				return true;

			return this.underlying.ContentEquals(attributes);
		}
		public override int GetHashCode() => this.underlying.GetHashCode();


		// IReadonlyDictionary members:
		public TValue this[TKey key] => underlying[key];
		public IEnumerable<TKey> Keys => underlying.Keys;
		public IEnumerable<TValue> Values => underlying.Values;
		public int Count => underlying.Count;
		public bool ContainsKey(TKey key) => underlying.ContainsKey(key);
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => underlying.GetEnumerator();
		public bool TryGetValue(TKey key, out TValue value) => underlying.TryGetValue(key, out value!);
		IEnumerator IEnumerable.GetEnumerator() => underlying.GetEnumerator();
	}
}
