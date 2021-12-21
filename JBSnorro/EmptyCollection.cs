using JBSnorro.Collections.ObjectModel;
using JBSnorro.Collections.Sorted;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JBSnorro
{
	/// <summary> A collection of empty collections. </summary>
	/// <typeparam name="T"> The type of the elements in the collections. </typeparam>
	public static class EmptyCollection<T>
	{
		/// <summary> Gets an empty array with elements of type T. </summary>
		public static readonly T[] Array = new T[0];
		/// <summary> Gets an empty ReadOnlyCollection with elements of type T. </summary>
		public static readonly ReadOnlyCollection<T> ReadOnlyList = new ReadOnlyCollection<T>(Array);
		/// <summary> Gets an empty ReadOnlyCollection with elements of type T. </summary>
		public static readonly MyReadOnlyObservableCollection<T> MyReadOnlyObservableCollection = new MyReadOnlyObservableCollection<T>(new ObservableCollection<T>());
		/// <summary> Gets an empty action. </summary>
		public static Action<T> Action = _ => { };
		/// <summary> Gets an empty enumerable. </summary>
		public static readonly IEnumerable<T> Enumerable = System.Linq.Enumerable.Empty<T>();
		/// <summary> Gets an empty sorted readonly list. </summary>
		public static readonly SortedReadOnlyList<T> SortedReadOnlyList = new SortedReadOnlyList<T>((IReadOnlyList<T>)ReadOnlyList, comparer);
		/// <summary> Gets a new empty list. </summary>
		public static List<T> List
		{
			get { return new List<T>(0); }
		}

		private static int comparer(T a, T b)
		{
			throw new InvalidOperationException("An empty sorted collection does not have an associated comparer. ");
		}
	}
}
