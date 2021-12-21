using JBSnorro;
using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
namespace JBSnorro
{
#if !NET5_0_OR_GREATER
	public static class ReferenceEqualityComparer
	{
		public static IEqualityComparer<object> Instance { get; } = InterfaceWraps.ToEqualityComparer<object>(ReferenceEquals, RuntimeHelpers.GetHashCode);
	}
#endif
	public static class Global
	{
		[DebuggerHidden]
		public static bool NotNull(object obj) => obj != null;
		[DebuggerHidden]
		public static Func<T, bool> not<T>(Func<T, bool> f) => arg => !f(arg);

		/// <summary>
		/// Gets an equality comparer that compares reference types by reference, and value types and strings by structural (default) equality.
		/// </summary>
		public static readonly IEqualityComparer<object> ReferenceAndStructuralEqualityComparer = createReferenceAndStructuralEqualityComparer();
		private static IEqualityComparer<object> createReferenceAndStructuralEqualityComparer()
		{
			return InterfaceWraps.ToEqualityComparer<object>(equals, getHashCode);

			bool equals(object x, object y)
			{
				if (x is ValueType || x is string)
				{
					var result = x.Equals(y);
					return result;
				}
				return ReferenceEquals(x, y);
			}
			int getHashCode(object x) => (x as ValueType ?? (object)(x as string))?.GetHashCode() ?? RuntimeHelpers.GetHashCode(x);
		}

		/// <summary>
		/// Create an equality comparer for type <typeparamref name="T"/> that always uses reference equality.
		/// </summary>
		public static IEqualityComparer<T> ReferenceEqualityComparerOf<T>() where T : class
		{
			return InterfaceWraps.ToEqualityComparer<T>(
#if DEBUG
				(T x, T y) =>
				{
					if (x is ValueType || y is ValueType)
						throw new ArgumentException("You shouldn't compare value types for reference");
					return ReferenceEquals(x, y);
				}
#else
				ReferenceEquals
#endif
				,
				RuntimeHelpers.GetHashCode);
		}

		/// <summary> Returns the specified sequence, or null of the specified sequence is empty (of course without quering the first element twice). </summary>
		public static IEnumerable<T> ToNullIfEmpty<T>(this IEnumerable<T> sequence)
		{
			Contract.Requires(sequence != null);
			using (var enumerator = sequence.GetEnumerator())
			{
				Contract.Requires(enumerator != null);

				if (!enumerator.MoveNext())
					return null;

				//now create a new enumerable with first the current element, and then the rest of the enumerator (so that the specified enumerable isn't enumerated over twice)
				return enumerator.Current.ToSingleton().Concat(enumerator.ToEnumerable(false));
			}
		}

		/// <summary> Evaluates the enumerable in DEBUG mode, ensuring that it is iterated over exactly once. 
		/// The resulting leaves is just a cache and can be enumerated over multiple times if desired. The result is null iff the argument is null. </summary>
		/// <typeparam name="T"> The type of the elements. </typeparam>
		/// <param name="sequence"> The elements to be evaluated (i.e. iterated over once and cached for further iterations). </param>
		[Conditional("DEBUG"), DebuggerHidden]
		public static void EnsureSingleEnumerationDEBUG<T>(ref IEnumerable<T> sequence)
		{
			// Contract.Ensures((Contract.OldValue(sequence) == null) == (Contract.NewValue(sequence) == null)); <see cref="Constract.Ensures"/>

			if (sequence != null)
				sequence = sequence.ToList();
		}
		/// <summary> Evaluates the enumerable in DEBUG mode, ensuring that it is iterated over exactly once. 
		/// The resulting leaves is just a cache and can be enumerated over multiple times if desired. The result is null iff the argument is null. </summary>
		/// <typeparam name="T"> The type of the elements. </typeparam>
		/// <param name="sequence"> The elements to be evaluated (i.e. iterated over once and cached for further iterations). </param>
		//[DebuggerHidden]
		public static IEnumerable<T> EnsureSingleEnumerationDEBUG<T>(this IEnumerable<T> sequence)
		{
#if DEBUG
			return sequence?.ToList();
#else
			return sequence;
#endif
		}
		/// <summary> Evaluates the enumerable in DEBUG mode, ensuring that it is iterated over exactly once. 
		/// The resulting leaves is just a cache and can be enumerated over multiple times if desired. The result is null iff the argument is null. 
		/// This out-parameter overload is provided to prevent PossibleMultipleEnumeration warnings at the caller site. </summary>
		/// <typeparam name="T"> The type of the elements. </typeparam>
		/// <param name="sequence"> The elements to be evaluated (i.e. iterated over once and cached for further iterations). </param>
		[DebuggerHidden]
		public static void EnsureSingleEnumerationDEBUG<T>(this IEnumerable<T> sequence, out IEnumerable<T> cachedSequence)
		{
			// Contract.Ensures((Contract.OldValue(sequence) == null) == (Contract.NewValue(sequence) == null)); <see cref="Contract.Ensures"/>

#if DEBUG
			cachedSequence = sequence?.ToList();
#else
			cachedSequence = sequence;
#endif
		}

		/// <summary> Evaluates the enumerable in DEBUG mode, ensuring that it is iterated over exactly once. 
		/// The resulting leaves is just a cache and can be enumerated over multiple times if desired. The result is null iff the argument is null. </summary>
		/// <typeparam name="T"> The type of the elements. </typeparam>
		/// <param name="sequence"> The elements to be evaluated (i.e. iterated over once and cached for further iterations). </param>
		[Conditional("DEBUG"), DebuggerHidden]
		public static void EnsureSingleEnumerationDEBUGSorted<T>(ref ISortedEnumerable<T> sequence)
		{
			if (sequence != null)
				sequence = new SortedList<T>(sequence.ToList(), sequence.Comparer);
		}

		/// <summary> The identity projection. </summary>
		[DebuggerHidden]
		public static T id<T>(T item) => item;

		/// <summary> Gets whether the preprocessor symbol DEBUG is present. </summary>
		[DebuggerHidden]
		public static bool DEBUG
		{
			get =>
#if DEBUG
				true;
#else
				false;
#endif
		}
		internal static readonly List<object> debugObjects = new List<object>();

		public static IReadOnlyList<object> DebugObjects => debugObjects;
		[Conditional("DEBUG")]
		public static void AddDebugObject(object obj)
		{
			debugObjects.Add(obj);
		}

		public static bool IsFileSystemCaseSensitive
		{
			get
			{
				return false;  // TODO: should be true for linux
			}
		}

	}
}
