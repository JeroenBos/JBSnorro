using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary> This dictionary represents a trivial dictionary where each key is associated to the same value. </summary>
	public class SingleValuedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		private readonly IEqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
		public TValue this[TKey key]
		{
			get
			{
				if (AssumeKeysExist)
					return Value;
				throw new NotImplementedException();
			}

			set
			{
				throw new InvalidOperationException();
				//alternative (though pretty ugly, not to mention the usage of the default EqualityComparer)
				//Contract.Assert(equalityComparer.Equals(value, Value), "This dictionary in actually readonly (but I would allow you to specify set the value that is already present");
			}
		}

		public bool AssumeKeysExist { get; }
		public TValue Value { get; }

		public SingleValuedDictionary(TValue value, bool assumeKeysExist)
		{
			if (!assumeKeysExist) throw new NotImplementedException("This type hasn't been implemented for this usage");

			Value = value;
			AssumeKeysExist = assumeKeysExist;
		}

		public int Count
		{
			get
			{
				if (AssumeKeysExist)
					throw new InvalidOperationException();
				throw new NotImplementedException();
			}
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public ICollection<TKey> Keys
		{
			get
			{
				if (AssumeKeysExist)
					throw new InvalidOperationException();
				throw new NotImplementedException();
			}
		}



		public ICollection<TValue> Values
		{
			get
			{
				if (AssumeKeysExist)
					throw new InvalidOperationException();
				throw new NotImplementedException();
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);

		}

		public void Add(TKey key, TValue value)
		{
			Contract.Requires(equalityComparer.Equals(value, Value));

			if (!AssumeKeysExist)
				throw new NotImplementedException();
		}

		public void Clear()
		{
			if (!AssumeKeysExist)
				throw new NotImplementedException();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return equalityComparer.Equals(Value, item.Value) && ContainsKey(item.Key);
		}

		public bool ContainsKey(TKey key)
		{
			if (AssumeKeysExist)
				return true;
			throw new NotImplementedException();
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if (AssumeKeysExist)
				throw new InvalidOperationException();
			throw new NotImplementedException();
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			if (AssumeKeysExist)
				throw new InvalidOperationException();
			throw new NotImplementedException();
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (AssumeKeysExist)
				throw new InvalidOperationException();
			throw new NotImplementedException();
		}

		public bool Remove(TKey key)
		{
			if (AssumeKeysExist)
				throw new InvalidOperationException();
			throw new NotImplementedException();
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			if (AssumeKeysExist)
				throw new InvalidOperationException();
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
