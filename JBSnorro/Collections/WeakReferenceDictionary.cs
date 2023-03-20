using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary>
	/// This class maps keys to objects, where the keys are references that are held weakly. 
	/// </summary>
	public sealed class WeakReferenceDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : class
	{
		private readonly IEqualityComparer<TKey> keyEqualityComparer;
		private readonly Dictionary<HashedWeakReference<TKey>, TValue> data;

		private int operationCount;
		private int operationCountToCleanOn;

		public WeakReferenceDictionary(IEqualityComparer<TKey> keyEqualityComparer, int autocleanUp = 10000)
		{
			Contract.Requires(autocleanUp > 0);

			this.operationCountToCleanOn = autocleanUp;
			this.keyEqualityComparer = keyEqualityComparer ?? EqualityComparer<TKey>.Default;
			data = new Dictionary<HashedWeakReference<TKey>, TValue>(new HashedWeakReferenceEqualityComparer<TKey>(this.keyEqualityComparer));
		}

		public TValue this[TKey key]
		{
			[DebuggerHidden]
			get
			{
				this.op();
				if (this.TryGetValue(key, out TValue result))
				{
					return result;
				}
				else
				{
					throw new KeyNotFoundException("The object with the specified key does not exist (anymore)");
				}
			}
			[DebuggerHidden]
			set
			{
				this.op();
				var hashedKey = new HashedWeakReference<TKey>(key, this.keyEqualityComparer);
				data[hashedKey] = value;
			}
		}


		public ICollection<TKey> Keys => this.Select(pair => pair.Key).ToReadOnlyList(); // does give a different reference each time. is that desired?
		public ICollection<TValue> Values => this.data.Values;

		[DebuggerHidden]
		public void Add(TKey key, TValue value)
		{
			this.op();
			var hashedKey = new HashedWeakReference<TKey>(key, this.keyEqualityComparer);
			this.data.Add(hashedKey, value);
		}
		/// <summary>
		/// Removes all entries whose weak reference has been garbage collected, thereby possibly freeing up the last reference to the value associated to that reference.
		/// </summary>
		public void Clean()
		{

			this.operationCount = 0;


			var valuesToRemove = this.data.Keys
										  .Where(key => !key.reference.TryGetTarget(out _))
										  .ToList();

			foreach (var valueToRemove in valuesToRemove)
				this.data.Remove(valueToRemove);

			//var garbageCollectedValue = this.data.Keys
			//                                     .Where(key => !key.reference.TryGetTarget(out _))
			//                                     .FirstOrDefault();
			//while (this.data.Remove(garbageCollectedValue))
			//{
			//    // because HashedWeakReferences equal each other if their weak references' targets have been collected
			//    // we can simply get one of those for which this is true, and call remove with that one
			//}
		}
		public void Clear()
		{
			this.operationCount = 0;
			this.data.Clear();
		}
		public bool ContainsKey(TKey key)
		{
			this.op();
			var hashedKey = new HashedWeakReference<TKey>(key, this.keyEqualityComparer);
			return this.data.ContainsKey(hashedKey);
		}
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			this.op();
			foreach (var pair in this.data)
			{
				if (pair.Key.reference.TryGetTarget(out TKey target))
				{
					yield return new KeyValuePair<TKey, TValue>(target, pair.Value);
				}
			}
		}
		public bool Remove(TKey key)
		{
			this.op();
			var hashedKey = new HashedWeakReference<TKey>(key, this.keyEqualityComparer);
			return this.data.Remove(hashedKey);
		}
		public bool TryGetValue(TKey key, out TValue value)
		{
			var hashedKey = new HashedWeakReference<TKey>(key, this.keyEqualityComparer);
			return this.TryGetValue(hashedKey, out value);
		}
		internal bool TryGetValue(HashedWeakReference<TKey> key, out TValue value)
		{
			this.op();

			return this.data.TryGetValue(key, out value);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			this.op();
			Add(item.Key, item.Value);
		}


		int ICollection<KeyValuePair<TKey, TValue>>.Count => this.data.Count;
		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			this.op();
			var hashedKey = new HashedWeakReference<TKey>(item.Key, this.keyEqualityComparer);
			return this.data.Contains(new KeyValuePair<HashedWeakReference<TKey>, TValue>(hashedKey, item.Value));
		}
		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			this.op();
			throw new NotImplementedException();
		}
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			this.op();
			var hashedKey = new HashedWeakReference<TKey>(item.Key, this.keyEqualityComparer);
			var mappedPair = new KeyValuePair<HashedWeakReference<TKey>, TValue>(hashedKey, item.Value);
			return ((IDictionary<HashedWeakReference<TKey>, TValue>)this.data).Remove(mappedPair);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}


		private void op()
		{
			this.operationCount++;
			if (operationCount == operationCountToCleanOn)
			{
				this.Clean();
			}
		}
	}
}
