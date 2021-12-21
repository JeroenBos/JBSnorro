using JBSnorro.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	/// <summary> Represents an index in a particular collection. </summary>
	/// <typeparam name="TElement"> The type of the elements the collection holds. </typeparam>
	public struct Index<TElement> : IGenericlessIndex
	{
		/// <summary> Gets the representation of an invalid index, that is, out of range or with special status. </summary>
		public static readonly Index<TElement> Invalid = new Index<TElement>();

		/// <summary> Gets the value of this index. </summary>
		public int Value { get; }
		/// <summary> Gets the collection in which this index points. </summary>
		public IReadOnlyList<TElement> Collection { get; }

		/// <summary> Gets the number of elements in the collection this index points in. </summary>
		public int CollectionCount
		{
			get { return Collection.Count; }
		}
		/// <summary> Gets the element this index points to. </summary>
		public TElement Element
		{
			get { return Collection[Value]; }
		}
		/// <summary> Gets whether this index points to the last element in the associated collection. </summary>
		public bool IsLast
		{
			get { return Value == CollectionCount - 1; }
		}
		/// <summary> Gets whether this index points to the first element in the associated collection. </summary>
		public bool IsFirst
		{
			get { return Value == 0 && this.Collection != null; }
		}
		/// <summary> Gets whether this is a valid index in an associated collection. </summary>
		public bool IsValid
		{
			get { return Collection != null; }
		}
		/// <summary> Gets whether this is an invalid index, that is, and index with special status or and index out of range of a particular collection (which isn't stored in <code>this.Collection</code>, btw). </summary>
		public bool IsInvalid
		{
			get { return !IsValid; }
		}


		/// <summary> Creates an index that points in an imaginary collection of objects selected per element in the specified collection. </summary>
		public static Index<TResult> CreateIndirectIndex<TResult>(int index, IReadOnlyList<TElement> collection, Func<TElement, TResult> selector)
		{
			return new Index<TResult>(index, new FacadeMapCollection<TElement, TResult>(collection, selector));
		}

		/// <summary> Creates a new index in the specified collection; or throws if the index is out of range. </summary>
		public Index(int index, IReadOnlyList<TElement> collection)
		{
			Contract.Requires(0 <= index);
			Contract.Requires(collection != null);
			Contract.Requires(index < collection.Count);

			this.Value = index;
			this.Collection = collection;
		}
		/// <summary> Creates a new index in the specified collection; or results in an invalid index if it is out of range. </summary>
		public Index(IReadOnlyList<TElement> collection, int possiblyInvalidIndex)
		{
			Contract.Requires(collection != null);

			if (0 <= possiblyInvalidIndex && possiblyInvalidIndex < collection.Count)
			{
				this = new Index<TElement>(possiblyInvalidIndex, collection);
			}
			else
			{
				this = Invalid;
			}
		}
		/// <summary> Creates a new index in the specified collection, namely that of the first element that matches the specified preciate; or the invalid index if no such element exists. </summary>
		public Index(IReadOnlyList<TElement> collection, Func<TElement, bool> indexOf) : this()
		{
			Contract.Requires(collection != null);
			Contract.Requires(indexOf != null);

			int i = collection.IndexOf(indexOf);

			if (i == -1)
			{
				this = Invalid;
			}
			else
			{
				this.Collection = collection;
				this.Value = i;
			}
		}
		/// <summary> Creates a new index in the specified collection, namely that of the specified element; or the invalid index of the element isn't in the specified collection. </summary>
		/// <param name="collection"> The collection to find the element in. </param>
		/// <param name="indexOf"> The element to find the index of. </param>
		/// <param name="equalityComparer"> The comparer determining whether the element has been found. Specify null to use the default equality comparer. </param>
		public Index(IReadOnlyList<TElement> collection, TElement indexOf, IEqualityComparer<TElement> equalityComparer = null)
			: this(collection, element => (equalityComparer ?? EqualityComparer<TElement>.Default).Equals(indexOf, element))
		{

		}

		/// <summary> Gets the next valid index in the associated collection, if any; or otherwise returns the invalid index. </summary>
		public static Index<TElement> operator ++(Index<TElement> index)
		{
			Contract.Requires(index.Collection != null);

			if (index.IsLast)
			{
				return Index<TElement>.Invalid;
			}
			else
			{
				Contract.Assert(index.Value < index.CollectionCount);
				return new Index<TElement>(index.Value + 1, index.Collection);
			}
		}
		/// <summary> Gets the offset index if it is valid; or otherwise returns the invalid index. </summary>
		public static Index<TElement> operator +(Index<TElement> index, int i)
		{
			Contract.Requires(index.IsValid);

			return new Index<TElement>(index.Collection, index.Value + i);
		}
		/// <summary> Gets the offset index if it is valid; or otherwise returns the invalid index. </summary>
		public static Index<TElement> operator +(int i, Index<TElement> index)
		{
			Contract.Requires(index.IsValid);

			return new Index<TElement>(index.Collection, index.Value + i);
		}
		/// <summary> Gets the offset index if it is valid; or otherwise returns the invalid index. </summary>
		public static Index<TElement> operator +(Index<TElement> a, Index<TElement> b)
		{
			Contract.Requires(a.IsValid);
			Contract.Requires(b.IsValid);
			Contract.Requires(a.Collection == b.Collection, "Can't add indices pointing in different collections");

			return new Index<TElement>(a.Collection, a.Value + b.Value);
		}
		/// <summary> Gets the offset index if it is valid; or otherwise returns the invalid index. </summary>
		public static Index<TElement> operator -(Index<TElement> a, Index<TElement> b)
		{
			Contract.Requires(a.IsValid);
			Contract.Requires(b.IsValid);
			Contract.Requires(a.Collection == b.Collection, "Can't add indices pointing in different collections");

			return new Index<TElement>(a.Collection, a.Value - b.Value);
		}

		/// <summary> Gets all indices in the specified collection. </summary>
		public static IEnumerable<Index<TElement>> Range(IReadOnlyList<TElement> collection)
		{
			Contract.Requires(collection != null);

			for (int i = 0; i < collection.Count; i++)
			{
				yield return new Index<TElement>(i, collection);
			}
		}
		/// <summary> Gets all indices in the specified collection starting at a particular index. </summary>
		public static IEnumerable<Index<TElement>> Range(IReadOnlyList<TElement> collection, Index<TElement> start)
		{
			Contract.Requires(collection != null);
			Contract.Requires(start.Collection == collection, "The specified start does not point in the specified collection");

			for (int i = start.Value; i < collection.Count; i++)
			{
				yield return new Index<TElement>(i, collection);
			}
		}
		/// <summary> Creates a collection of indices pointing in the specified collection. </summary>
		/// <param name="collection"> The collection the resulting indices will point in. </param>
		/// <param name="indices"> The indices to point at. Must be in range. </param>
		public static Index<TElement>[] Range(IReadOnlyList<TElement> collection, params int[] indices)
		{
			Contract.Requires(collection != null);
			Contract.Requires(indices != null);
			Contract.RequiresForAll(indices, i => 0 <= i && i < collection.Count);

			return indices.Map(i => new Index<TElement>(i, collection));
		}

		/// <summary> Maps (surjectively) the current index to an index pointing to some property of the elements in the specified collection. </summary>
		// In other words: Creates an index that points in an imaginary collection of objects selected per element in the specified collection. </summary>
		public Index<TResult> Map<TResult>(Func<TElement, TResult> selector)
		{
			Contract.Requires(selector != null);
			Contract.Requires(this.IsValid);

			return CreateIndirectIndex(this.Value, this.Collection, selector);
		}
		public Index<TResult> Map<TResult>(IReadOnlyList<TResult> homomorhpCollection)
		{
			return new Index<TResult>(this.Value, homomorhpCollection);
		}

		public override string ToString()
		{
			return IsValid ? $"{Value} in {Collection}" : "Invalid index";
		}

		#region Equality Members

		IReadOnlyList<object> IGenericlessIndex.Collection => (IReadOnlyList<object>)Collection;//doesn't work for structs

		/// <summary> Gets whether the two specified indices point in the same collection to the same element, or whether they are both invalid. </summary>
		public static bool operator ==(Index<TElement> a, Index<TElement> b)
		{
			return a.Equals(b);
		}
		/// <summary> Gets whether (exactly) one of the two specified indices is invalid, or whether they point in the different collections or whether the point to the different elements. </summary>
		public static bool operator !=(Index<TElement> a, Index<TElement> b)
		{
			return !(a == b);
		}
		public override bool Equals(object obj)
		{
			var index = obj as IGenericlessIndex;
			if (index == null)
				return false;
			return index.Collection == this.Collection && index.Value == this.Value;
		}
		public bool Equals<T>(Index<T> index)
		{
			return index.Collection == this.Collection && index.Value == this.Value;
		}
		public override int GetHashCode()
		{
			throw new NotImplementedException();//to prevent C# warning
		}

		#endregion
	}

	interface IGenericlessIndex
	{
		int Value { get; }
		IReadOnlyList<object> Collection { get; }
	}
	interface IGenericlessCount
	{
		IReadOnlyList<object> Collection { get; }
	}

	public struct Count<TElement> : IGenericlessCount
	{
		public IReadOnlyList<TElement> Collection { get; }
		public int Value
		{
			get { return Collection.Count; }
		}

		public Count(IReadOnlyList<TElement> collection)
		{
			Contract.Requires(collection != null);

			Collection = collection;
		}

		public Count<TResult> Map<TResult>()
		{
			return new Count<TResult>(new FacadeMapCollection<TElement, TResult>(this.Collection, _ => default(TResult)));
		}

		public static bool operator >=(Count<TElement> count, int i)
		{
			return count.Value >= i;
		}
		public static bool operator <=(Count<TElement> count, int i)
		{
			return count.Value <= i;
		}
		public static bool operator ==(Count<TElement> count, int i)
		{
			return count.Value == i;
		}
		public static bool operator !=(Count<TElement> count, int i)
		{
			return count.Value != i;
		}
		public static bool operator >(Count<TElement> count, int i)
		{
			return count.Value > i;
		}
		public static bool operator <(Count<TElement> count, int i)
		{
			return count.Value < i;
		}
		public static bool operator >=(int i, Count<TElement> count)
		{
			return i >= count.Value;
		}
		public static bool operator <=(int i, Count<TElement> count)
		{
			return i <= count.Value;
		}
		public static bool operator ==(int i, Count<TElement> count)
		{
			return i == count.Value;
		}
		public static bool operator !=(int i, Count<TElement> count)
		{
			return i != count.Value;
		}
		public static bool operator >(int i, Count<TElement> count)
		{
			return i > count.Value;
		}
		public static bool operator <(int i, Count<TElement> count)
		{
			return i < count.Value;
		}

		#region Equality Members 

		IReadOnlyList<object> IGenericlessCount.Collection => (IReadOnlyList<object>)Collection;

		public override bool Equals(object obj)
		{
			var index = obj as IGenericlessIndex;
			if (index == null)
				return false;
			return index.Collection == this.Collection && index.Value == this.Value;
		}
		public override int GetHashCode()
		{
			throw new NotImplementedException();//to prevent C# warning
		}

		#endregion
	}
}
