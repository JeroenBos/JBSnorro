using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Collections
{
	/// <summary>
	/// Represents a dictionary from one value type to a weakly referenced reference type.
	/// </summary>
	public class WeakTwoWayDictionary<TKey, TValue> where TKey : struct where TValue : class
	{
		private readonly IEqualityComparer<TValue> equalityComparer;
		private readonly WeakReferenceDictionary<TValue, TKey> valueToKey;
		private readonly Dictionary<TKey, WeakReference<TValue>> keyToValue;

		private int operationCount;
		private int operationCountToCleanOn;

		/// <param name="autoCleanup"> After the specified number of calls on this instance, it's internal state is cleaned. </param>
		public WeakTwoWayDictionary(IEqualityComparer<TValue>? equalityComparer = null, int autoCleanup = 10000)
		{
			if (autoCleanup < 1000) { throw new ArgumentException($"'{nameof(autoCleanup)}' must be at least 1000"); } // specify int.MaxValue to disable auto cleanup

			this.equalityComparer = equalityComparer ?? EqualityComparer<TValue>.Default;
			this.valueToKey = new WeakReferenceDictionary<TValue, TKey>(this.equalityComparer, int.MaxValue);
			this.keyToValue = new Dictionary<TKey, WeakReference<TValue>>();
			this.operationCountToCleanOn = autoCleanup;
		}

		/// <summary>
		/// Gets the key associated to the specified value. Throws a <see cref="KeyNotFoundException"/> if the value is not in this dictionary.
		/// </summary>
		/// <exception cref="KeyNotFoundException"> If the value is not in this dictionary. </exception>
		public TKey this[TValue value]
		{
			get
			{
#if DEBUG
				var objectTypesInThisDictionary = this.keyToValue.Values.Select(entry => entry?.GetType()).Distinct().ToList();
				var valueType = value?.GetType();
				bool isProbablyRightType = objectTypesInThisDictionary.Contains(valueType);
				if (!isProbablyRightType)
				{
					//    Console.Out.WriteLineAsync("Probably a wrong argument type was specified");
				}
#endif
				this.op();
				return this.valueToKey[value!];
			}
		}
		/// <summary>
		/// Gets the value associated to the specified key. Throws a <see cref="KeyNotFoundException"/> if the key is not in this dictionary.
		/// </summary>
		/// <exception cref="KeyNotFoundException"> If the key is not in this dictionary. </exception>
		public TValue this[TKey key]
		{
			get
			{
				if (this.TryGetValue(key, out TValue result))
				{
					return result;
				}
				else
				{
					throw new KeyNotFoundException("The object with the specified id does not exist");
				}
			}
		}

		public bool TryGetKey(TValue value, out TKey key)
		{
			this.op();
			return this.valueToKey.TryGetValue(value, out key);
		}
		public bool TryGetValue(TKey key, out TValue value)
		{
			this.op();
			if (this.keyToValue.TryGetValue(key, out var valueReference))
			{
				if (valueReference.TryGetTarget(out value!))
				{
					return true;
				}
				else
				{
					this.keyToValue.Remove(key);
				}
			}
			value = default!;
			return false;
		}

		/// <summary>
		/// Adds the specified pair to this weak dictionary. Throws if the key or value is already present.
		/// </summary>
		public void Add(TKey key, TValue value)
		{
			this.op();

			this.valueToKey.Add(value, key);
			this.keyToValue.Add(key, new WeakReference<TValue>(value));
			// TODO: build an advanced recovery scenario where second statement throws and we have to undo the first statement
		}
		public void Remove(TKey key)
		{
			this.op();
			if (this.keyToValue.TryGetValue(key, out var weakRef))
			{
				this.keyToValue.Remove(key);
				if (weakRef.TryGetTarget(out TValue? value))
				{
					this.valueToKey.Remove(value);
				}
			}
		}
		public void Remove(TValue value)
		{
			this.op();

			if (this.valueToKey.TryGetValue(value, out TKey key))
			{
				this.valueToKey.Remove(value);
				this.keyToValue.Remove(key);
			}
			else
			{
				Contract.Assert(!this.keyToValue.Values
												.Where(weakRef => weakRef.TryGetTarget(out _))
												.Select(weakRef => { weakRef.TryGetTarget(out var obj2); return obj2; })
												.Contains(value), "The dictionaries aren't symmetric");
			}
		}
		public void Remove(TKey key, TValue value)
		{
			this.op();
			this.valueToKey.Remove(value);
			this.keyToValue.Remove(key);
		}
		public void Clean()
		{
			this.operationCount = 0;
			this.valueToKey.Clean();

			var keysToRemove = this.keyToValue
								   .Where(t => !t.Value.TryGetTarget(out _))
								   .Select(t => t.Key)
								   .ToList();
			foreach (TKey idToRemove in keysToRemove)
			{
				this.keyToValue.Remove(idToRemove);
			}
		}

		public bool Contains(TValue value)
		{
			this.op();
			return this.valueToKey.ContainsKey(value);
		}
		public bool Contains(TKey key)
		{
			this.op();
			// returns whether it is in this collection and whether the referred to object hasn't been GCd yet. 
			if (this.keyToValue.TryGetValue(key, out var weakRef))
			{
				if (weakRef.TryGetTarget(out _))
					return true;
				else
					this.keyToValue.Remove(key);
			}
			return false;
		}

		private void op()
		{
			this.operationCount++;
			if (operationCount > operationCountToCleanOn)
			{
				this.Clean();
			}
		}
	}

	internal struct HashedWeakReference<T> where T : class
	{
		public readonly int hashCode;
		public readonly WeakReference<T> reference;

		public HashedWeakReference(T obj, IEqualityComparer<T> getHashCode)
		{
			if (obj == null) { throw new NullReferenceException(nameof(obj)); }
			if (getHashCode == null) { throw new NullReferenceException(nameof(getHashCode)); }

			this.hashCode = getHashCode.GetHashCode(obj);
			this.reference = new WeakReference<T>(obj);
		}

		public override int GetHashCode()
		{
			throw new InvalidOperationException($"{nameof(HashedWeakReferenceEqualityComparer<T>)} should be used. ");
		}
		public override bool Equals(object? obj)
		{
			throw new InvalidOperationException($"{nameof(HashedWeakReferenceEqualityComparer<T>)} should be used. ");
		}
	}
	internal sealed class HashedWeakReferenceEqualityComparer<T> : IEqualityComparer<HashedWeakReference<T>> where T : class
	{
		private readonly IEqualityComparer<T> equalityComparer;
		public HashedWeakReferenceEqualityComparer(IEqualityComparer<T> equalityComparer)
		{
			if (equalityComparer == null) { throw new ArgumentNullException(nameof(equalityComparer)); }

			this.equalityComparer = equalityComparer;
		}

		public bool Equals(HashedWeakReference<T> ref1, HashedWeakReference<T> ref2)
		{
			bool target1Exists = ref1.reference.TryGetTarget(out T? value1);
			bool target2Exists = ref2.reference.TryGetTarget(out T? value2);

			if (!target1Exists && !target2Exists)
				return true;

			return target1Exists && target2Exists && this.equalityComparer.Equals(value1, value2);
		}

		public int GetHashCode(HashedWeakReference<T> obj)
		{
			return obj.hashCode;
		}
	}
}
