using JBSnorro.Collections.Sorted;
#nullable disable
using JBSnorro.Diagnostics;
using System.Collections;

namespace JBSnorro.Collections;

/// <summary> Represents a mutable dictionary where the values are accessible through a key, but are also stored orderedly. </summary>
public class DictionarySortedValues<TKey, TValue> : IDictionary<TKey, TValue>
{
	/// <summary> The dictionary relating keys to indices in the sorted list of values. </summary>
	private readonly Dictionary<TKey, int> indices;
	/// <summary> The underlying sorted list of values of this dictionary. </summary>
	private readonly SortedList<TValue> values;
	/// <summary> Gets the sorted list of values in this dictionary. </summary>
	public SortedReadOnlyList<TValue> Values { get; }
	/// <summary> Gets the list of keys in this collection. </summary>
	public Dictionary<TKey, int>.KeyCollection Keys { get; }
	/// <summary> Gets or sets a key-value pair in this dictionary. </summary>
	/// <param name="key"> The key of the value to get, or the key to associated the set value with. When setting, adds if the key is not present. </param>
	public TValue this[TKey key]
	{
		get { return values[indices[key]]; }
		set
		{
			int index;
			if (indices.TryGetValue(key, out index))
				indices[key] = values.Change(index, value);
			else
				Add(key, value);//like the standard dictionary, the value is added if the key is not present
		}
	}

	/// <summary> Notifies this dictionary that the value associated with the specified key may have changed, and hence may need resorting.
	/// Does nothing if the key is not present. </summary>
	/// <param name="key"> The key associated with the value that may have changed. </param>
	public void OnValueChanged(TKey key)
	{
		int index;
		if (this.indices.TryGetValue(key, out index))
		{
			this.values.OnValueChanged(index);
		}
	}


	/// <summary> Creates a new dictionary where the values are stored sorted. </summary>
	/// <param name="capacity"> The initial capacity of this dictionary. </param>
	/// <param name="keyEqualityComparer"> The equality comparer for the keys. </param>
	/// <param name="valueComparer"> The comparer determining the order of the values. Specify null to use the default comparer. </param>
	public DictionarySortedValues(Func<TValue, TValue, int> valueComparer = null, int capacity = 4, IEqualityComparer<TKey> keyEqualityComparer = null)
	{
		this.values = new SortedList<TValue>(valueComparer, capacity);
		this.indices = new Dictionary<TKey, int>(capacity, keyEqualityComparer ?? EqualityComparer<TKey>.Default);
		this.Keys = indices.Keys;
		this.Values = new SortedReadOnlyList<TValue>(values);
	}

	/// <summary> Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>, if no such key exists already. </summary>
	/// <param name="key">The object to use as the key of the element to add.</param><param name="value">The object to use as the value of the element to add.</param>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
	/// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</exception>
	/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
	public void Add(TKey key, TValue value)
	{
		indices.Add(key, this.Count);
		int index = values.Add(value);
		indices[key] = index;
	}
	/// <summary> Removes the specified key and associated value from this dictionary. Returns whether the key was found, and with that, removed. </summary>
	/// <param name="key"> The key to remove. </param>
	public bool Remove(TKey key)
	{
		int valueIndex;
		if (this.indices.TryGetValue(key, out valueIndex))
		{
			this.indices.Remove(key);
			values.RemoveAt(valueIndex);
			return true;
		}
		return false;
	}
	/// <summary> Tries to get the value associated with the specified key. Returns whether the key was present. </summary>
	/// <param name="key"> The key to try to get the associated value with. </param>
	/// <param name="value"> The value associated with the key, if any. </param>
	/// <returns> whether the key was found. </returns>
	public bool TryGetValue(TKey key, out TValue value)
	{
		int valueIndex;
		if (this.indices.TryGetValue(key, out valueIndex))
		{
			value = values[valueIndex];
			return true;
		}
		else
		{
			value = default(TValue);
			return false;
		}
	}

	#region Trivial members

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		foreach (TKey key in this.Keys)
			yield return new KeyValuePair<TKey, TValue>(key, this[key]);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	/// <summary> Adds the key-value pair to this dictionary. </summary>
	/// <param name="item"> The key-value pair to add. </param>
	public void Add(KeyValuePair<TKey, TValue> item)
	{
		this.Add(item.Key, item.Value);
	}

	/// <summary> Removes all keys and values from this dictionary. </summary>
	public void Clear()
	{
		indices.Clear();
		values.Clear();
	}

	/// <summary> Returns whether this dictionary has the specified key with associated value the specified value. </summary>
	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		TValue associatedValue;
		bool containsKey = TryGetValue(item.Key, out associatedValue);
		if (containsKey)
		{
			bool valuesEqual = values.Comparer(associatedValue, item.Value) == 0;
			return valuesEqual;
		}
		else
		{
			return false;
		}
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		Contract.Requires(array != null);
		Contract.Requires(0 <= arrayIndex);
		Contract.Requires(arrayIndex + this.Count < array.Length, "Doesn't fit in specified array");

		foreach (TKey key in this.Keys)
			array[arrayIndex++] = new KeyValuePair<TKey, TValue>(key, this[key]);
	}

	/// <summary> Removes the key-value pair from this dictionary if it is contained in this dictionary. Returns whether it was removed. </summary>
	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		TValue associatedValue;
		bool containsKey = TryGetValue(item.Key, out associatedValue);
		if (containsKey)
		{
			bool valuesEqual = values.Comparer(associatedValue, item.Value) == 0;
			if (valuesEqual)
			{
				Remove(item.Key);
				return true;
			}
		}
		return false;
	}
	/// <summary> Gets the number of key-value pairs in this dictionary. </summary>
	public int Count
	{
		get { return this.Keys.Count; }
	}

	public bool IsReadOnly
	{
		get { return false; }
	}

	/// <summary> Gets whether this dictionary contains the specified key. </summary>
	public bool ContainsKey(TKey key)
	{
		return this.Keys.Contains(key);
	}
	/// <summary> Gets whether this dictionary contains the specified value. </summary>
	public bool ContainsValue(TValue value)
	{
		return values.Contains(value);
	}

	ICollection<TKey> IDictionary<TKey, TValue>.Keys
	{
		get { return Keys; }
	}
	ICollection<TValue> IDictionary<TKey, TValue>.Values
	{
		get { return values; }
	}

	#endregion
}
