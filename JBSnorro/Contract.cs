using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Global

namespace JBSnorro.Diagnostics
{
	/// <summary>
	/// This class helps in debugging (for now: I'll have to find a better alternative later) things depending on focus.
	/// </summary>
	public static class Debug
	{
		public static bool Flag;
		public static void Break()
		{
		}
		public static void BreakIfFlagIsSet()
		{
			if (Flag)
			{
			}
		}
	}
	public sealed class InvariantMethodAttribute : Attribute { }

	public static class Contract
	{
		[Conditional("DEBUG")]
		[DebuggerHidden]
		[ContractAnnotation("halt <= requirement: false")]
		[AssertionMethod("requirement")]
		public static void Requires([DoesNotReturnIf(false)] bool requirement, string message = "Precondition failed: '{0}'", [CallerArgumentExpression("requirement")] string callerExpression = "")
		{
			if (!requirement) throw new ContractException(string.Format(message, callerExpression));
		}

		[Conditional("DEBUG")]
		[DebuggerHidden]
		[ContractAnnotation("halt <= requirement: false")]
		[AssertionMethod("requirement")]
		public static void Requires<TException>([DoesNotReturnIf(false)] bool requirement, params object[] arguments) where TException : Exception
		{
			if (requirement)
				return;
			var ctor = typeof(TException).GetConstructor(arguments.Select(arg => arg.GetType()).ToArray());
			if (ctor == null) throw new ArgumentException("arguments", string.Format("Couldn't find a constructor of type {0} for the specified arguments", typeof(TException)));
			var exception = ctor.Invoke(arguments);
			throw (TException)exception;
		}

		[Conditional("DEBUG")]
		[DebuggerHidden]
		[ContractAnnotation("halt <= assertion: false")]
		[AssertionMethod("assertion")]
		public static void Assert([DoesNotReturnIf(false)] bool assertion, string message = "Assertion failed: '{0}'", [CallerArgumentExpression("assertion")] string callerExpression = "")
		{
			if (!assertion) throw new ContractException(string.Format(message, callerExpression));
		}
		[Conditional("DEBUG")]
		[DebuggerHidden]
		[ContractAnnotation("halt <= assertion: false")]
		[AssertionMethod("assertion")]
		public static void Assert<TException>([DoesNotReturnIf(false)] bool assertion, string message = "Assertion failed: '{0}'", [CallerArgumentExpression("assertion")] string callerExpression = "") where TException : Exception
		{
			if (!assertion) throw Create<TException>(string.Format(message, callerExpression));
		}

		private static TException Create<TException>(string message)
		{
			var ctor = typeof(TException).GetConstructor(new[] { typeof(string) });
			return (TException)ctor.Invoke(new[] { message });
		}
		/// <summary> Denotes that the caller shouldn't be possible to reach. </summary>
		[DebuggerHidden]
		[ContractAnnotation("=> halt")]
		[DoesNotReturn]
		public static void Throw()
		{
			throw new ContractException("Shouldn't be possible");
		}
		/// <summary> Denotes that the caller shouldn't be possible to reach. </summary>
		[DebuggerHidden]
		[ContractAnnotation("=> halt")]
		[DoesNotReturn]
		public static void Fail()
		{
			throw new ContractException("Shouldn't be possible");
		}

		[DebuggerHidden]
		public static T Result<T>()
		{
			return default(T);
		}

		[DebuggerHidden, Conditional("DEBUG"), AssertionMethod("assertion")]
		public static void Ensures([DoesNotReturnIf(false)] bool postcondition, string message = "Postcondition failed: '{0}'", [CallerArgumentExpression("postcondition")] string callerExpression = "")
		{
			if (!postcondition)
			{
				throw new ContractException(string.Format(message, callerExpression));
			}
		}

		[DebuggerHidden, Conditional("DEBUG")]
		public static void RequiresForAll<T>(IEnumerable<T> elements, Func<T, bool> predicate, string message = "Requirement not met")
		{
			Contract.Requires(predicate != null);
			Contract.RequiresForAll(elements, [DebuggerHidden] (elem, _) => predicate(elem), message);
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void RequiresForAll<T>(IEnumerable<T> elements, Func<T, int, bool> predicate, string message = "Precondition for element at index {0} failed")
		{
			Contract.Requires(elements != null);
			Contract.RequiresIsNotEnumeratorEncapsulate(elements);

			AssertForAll(elements, predicate, message);
		}

		[DebuggerHidden, Conditional("DEBUG")]
		public static void RequiresForAll<T>(IEnumerable<T> elements, Action<T> assertion)
		{
			Contract.Requires(elements != null);
			Contract.RequiresIsNotEnumeratorEncapsulate(elements);

			foreach (T element in elements)
			{
				assertion(element);
			}
		}

		[DebuggerHidden, Conditional("DEBUG")]
		public static void RequiresWindowed2<T>(IEnumerable<T> elements, Func<T, T, bool> windowedPredicate, string message = "Correlation requirement not met between elements at index {0} and the next")
		{
			int i = 0;
			bool condition = elements.Windowed2().All(tuple => { i++; return windowedPredicate(tuple.First, tuple.Second); });
			Contract.Requires(condition, string.Format(message, i, i + 1));
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void RequiresForAny<T>(IEnumerable<T> elements, Func<T, bool> predicate, string message = "Requirement not met for any element", [CallerArgumentExpression("predicate")] string callerExpression = "")
		{
			Contract.Requires(elements != null);
			Contract.RequiresIsNotEnumeratorEncapsulate(elements);

			Requires(elements.Any(predicate), message, callerExpression);
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void RequiresForAny<T>(ref IEnumerable<T> elements, Func<T, bool> predicate, string message = "Requirement not met for any element", [CallerArgumentExpression("predicate")] string callerExpression = "")
		{
			Contract.Requires(elements != null);


			elements = elements.EnsureSingleEnumerationDEBUG();
			Requires(elements.Any(predicate), message, callerExpression);
		}
		[DebuggerHidden]
		private static void RequiresIsNotEnumeratorEncapsulate<T>(IEnumerable<T> elements)
		{
			if (elements.GetType() == typeof(EnumeratorWrapper<T>))
			{
				throw new ContractException("You're probably calling RequiresForAll while you should be calling LazilyRequire, as you already lazily required something else on the specified sequence");
			}
		}

		/// <summary> Requires a sequential correlation between the two sequences. </summary>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void RequiresCorrelation<T, U>(IEnumerable<T> sequence1, IEnumerable<U> sequence2, Func<T, U, bool> predicate, bool requireEqualLength = true, string description = "Elements '{0}' and '{1}' at index {2} do not match the predicate")
		{
			Contract.Requires(sequence1 != null);
			Contract.Requires(sequence2 != null);
			Contract.Requires(predicate != null);

			int i = 0;
			using (var enumerator1 = sequence1.GetEnumerator())
			using (var enumerator2 = sequence2.GetEnumerator())
			{
				bool enumerator1HasMore = enumerator1.MoveNext();
				bool enumerator2HasMore = enumerator2.MoveNext();
				while (enumerator1HasMore && enumerator2HasMore)
				{
					if (requireEqualLength && enumerator1HasMore != enumerator2HasMore)
						throw new ContractException("The specified sequences are of unequal length");

					Contract.Assert(predicate(enumerator1.Current, enumerator2.Current), string.Format(description, enumerator1.Current, enumerator2.Current, i));
					i++;

					enumerator1HasMore = enumerator1.MoveNext();
					enumerator2HasMore = enumerator2.MoveNext();
				}
			}
		}

		[DebuggerHidden, Pure]
		public static bool ForAll<T>(IEnumerable<T> elements, Func<T, bool> predicate)
		{
			Contract.Requires(predicate != null);

			return ForAll(elements, (element, i) => predicate(element));
		}
		[DebuggerHidden, Pure]
		public static bool ForAll<T>(IEnumerable<T> elements, Func<T, int, bool> predicate)
		{
			int i = 0;
			foreach (var element in elements)
				if (!predicate(element, i++))
					return false;
			return true;
		}
#nullable enable
		[DebuggerHidden, Pure]
		public static bool AllNotNull<T>(IEnumerable<T?> elements)
        {
			return ForAll(elements, element => element != null);
		}
#nullable restore
		[DebuggerHidden, Pure]
		public static bool ForAllCombinations<T>(IEnumerable<T> elements, Func<T, T, bool> predicate)
		{
			foreach (var element in elements.ToList().AllCombinations(2))
				if (!predicate(element[0], element[1]))
					return false;
			return true;
		}
		[DebuggerHidden]
		[ContractAnnotation("halt <= assumption: false")]
		[AssertionMethod("assumption")]
		public static void Assume([DoesNotReturnIf(false)] bool assumption, string message = "False assumption: '{0}'", [CallerArgumentExpression("assumption")] string callerExpression = "")
		{
			if (!assumption)
				throw new ContractException(string.Format(message, callerExpression));
		}
		[DebuggerHidden]
		[ContractAnnotation("halt <= invariant: false")]
		[AssertionMethod("assumption")]
		public static void Invariant([DoesNotReturnIf(false)] bool invariant, string message = "Invariant broken: '{0}'", [CallerArgumentExpression("invariant")] string callerExpression = "")
		{
			if (!invariant)
				throw new ContractException(string.Format(message, callerExpression));
		}
		[DebuggerHidden]
		public static void InvariantForAll<T>(IEnumerable<T> sequence, Func<T, bool> predicate, string message = "Invariant broken: '{0}'", [CallerArgumentExpression("predicate")] string callerExpression = "")
		{
			InvariantForAll(sequence, [DebuggerHidden] (element, i) => predicate(element), message, callerExpression);
		}
		[DebuggerHidden]
		public static void InvariantForAll<T>(IEnumerable<T> sequence, Func<T, int, bool> predicate, string message = "Invariant broken: '{0}[{1}]'", [CallerArgumentExpression("predicate")] string callerExpression = "")
		{
			foreach (var (element, i) in sequence.WithIndex())
			{
				Invariant(predicate(element, i), string.Format(message, callerExpression, i));
			}
		}

		[DebuggerHidden]
		public static T NewValue<T>(T leaves)
		{
			return default(T);
		}
		[DebuggerHidden]
		public static T OldValue<T>(T leaves)
		{
			return default(T);
		}
		/// <summary> Asserts the predicate for all elements in the specified sequence. </summary>
		/// <param name="sequence"> The elements to check for matching the predicate. </param>
		/// <param name="predicate"> The predicate function to be asserted for all elements. </param>
		/// <param name="message"> The error message to be shown on an assertion failure. {0} is the index of the element that failed the assertion. </param>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void AssertForAll<T>(this IEnumerable<T> sequence, Func<T, bool> predicate, string message = "The element at index {0} does not match the predicate", [CallerArgumentExpression("predicate")] string callerExpression = "")
		{
			sequence.AssertForAll((element, i) => predicate(element), message, callerExpression);
		}
		/// <summary> Asserts the predicate for all elements in the specified sequence. </summary>
		/// <param name="sequence"> The elements to check for matching the predicate. </param>
		/// <param name="predicate"> The predicate function to be asserted for all elements. </param>
		/// <param name="message"> The error message to be shown on an assertion failure. {0} is the index of the element that failed the assertion. </param>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void AssertForAll<T>(this IEnumerable<T> sequence, Func<T, int, bool> predicate, string message = "Assertion failed: '{0}[{1}]'", [CallerArgumentExpression("predicate")] string callerExpression = "")
		{
			Requires(sequence != null);
			Requires(predicate != null);
			Requires(message != null);

			int i = 0;
			foreach (T element in sequence)
			{
				if (!predicate(element, i))
					throw new ContractException(string.Format(message, callerExpression, i));
				i++;
			}
		}
		/// <summary> Documents an assumption, on elements in the specified sequence, used by the calling code. </summary>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void AssumeForAll<T>(this IEnumerable<T> sequence, Func<T, bool> predicate, string message = "False assumption: '{0}[{1}]'", [CallerArgumentExpression("predicate")] string callerExpression = "")
		{
			AssertForAll(sequence, predicate, message, callerExpression);
		}

		/// <summary> Asserts that the specified predicate holds for all elements and throws otherwise. </summary>
		/// <param name="sequence"> The elements to check for matching the predicate. </param>
		/// <param name="predicate"> The predicate function to be asserted for all elements. </param>
		/// <param name="message"> The error message to be shown on an assertion failure. {0} is the index of the element that failed the assertion. </param>
		/// <returns> the original sequence</returns>
		[DebuggerHidden]
		public static IEnumerable<T> LazilyAssertForAll<T>(this IEnumerable<T> sequence, Func<T, bool> predicate, string message = "Assertion failed: '{0}[{1}]'", [CallerArgumentExpression("predicate")] string callerExpression = "")
		{
#if DEBUG
			Requires(sequence != null);
			Requires(predicate != null);
			Requires(message != null);

			int i = 0;
			foreach (T element in sequence)
			{
				if (!predicate(element))
					throw new ContractException(string.Format(message, callerExpression, i));
				i++;
				yield return element;
			}
#else
			return sequence;
#endif
		}
		/// <summary> Lazily asserts for all elements in the specified sequence that the predicate holds. </summary>
		public static void LazilyAssertForAll<T>(ref IEnumerable<T> sequence, Func<T, bool> predicate)
		{
			sequence = sequence.LazilyAssertForAll(predicate);
		}

		/// <summary> Asserts that the specified predicate holds for all elements and throws otherwise. </summary>
		/// <param name="sequence"> The elements to check for matching the predicate. </param>
		/// <param name="assertion"> The asserting function to be applied to all elements. </param>
		/// <returns> the original sequence. </returns>
		[DebuggerHidden]
		public static IEnumerable<T> LazilyAssertForAll<T>(this IEnumerable<T> sequence, Action<T> assertion)
		{
			foreach (T element in sequence)
			{
				assertion(element);
				yield return element;
			}
		}

		//the following three methods have been inspired by the mostly annoying but sometimes useful warning of Resharper on 'possible multiple enumeration of IEnumerable'
		/// <summary> Asserts that the specified sequence has at least the specified minimum number of elements. This is only done upon enumeration of the enumerable, unless the runtime type is of ICollection. </summary>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyAssertMinimumCount<T>(ref IEnumerable<T> sequence, int minimumCount)
		{
			Requires(sequence != null);
			Requires(minimumCount >= 0);//minimumCount == 0 is strange, but not necessarily forbidden

			//first try to assert immediately (non-lazily)
			var list = sequence as ICollection;
			if (list != null)
			{
				if (list.Count < minimumCount)
					CountAssertionEnumerator<T>.ThrowMinimumNotAttainedException(minimumCount, list.Count);
				return;
			}

			//replace sequence with a minimum-count-asserting-sequence
			sequence = CountAssertionEnumerator<T>.Minimum(sequence, minimumCount).ToEnumerable(false).EnsureSingleEnumerationDEBUG();
		}
		/// <summary> Asserts that the specified sequence has at most the specified minimum number of elements. This is only done upon enumeration of the enumerable, unless the runtime type is of ICollection. </summary>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyAssertMaximumCount<T>(ref IEnumerable<T> sequence, int maximumCount)
		{
			//first try to assert immediately (non-lazily)
			var list = sequence as ICollection;
			if (list != null)
			{
				if (list.Count < maximumCount)
					CountAssertionEnumerator<T>.ThrowMaximumExceededException(maximumCount, list.Count);
				return;
			}

			//replace sequence with a maximum-count-asserting-sequence
			sequence = CountAssertionEnumerator<T>.Maximum(sequence, maximumCount).ToEnumerable(false).EnsureSingleEnumerationDEBUG();
		}

		/// <summary> Asserts that the specified sequence has exactly the specified minimum number of elements. This is only done upon enumeration of the enumerable, unless the runtime type is of ICollection. </summary>
		/// <param name="requiredCount"> The required exact number of elements in the specified sequence. </param>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyAssertCount<T>(ref IEnumerable<T> sequence, int requiredCount)
		{
			//first try to assert immediately (non-lazily)
			var list = sequence as ICollection;
			if (list != null)
			{
				if (list.Count > requiredCount)
					CountAssertionEnumerator<T>.ThrowMinimumNotAttainedException(requiredCount, list.Count);
				if (list.Count < requiredCount)
					CountAssertionEnumerator<T>.ThrowMaximumExceededException(requiredCount, list.Count);
				return;
			}

			sequence = CountAssertionEnumerator<T>.Exactly(sequence, requiredCount).ToEnumerable(false).EnsureSingleEnumerationDEBUG();
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyAssertSortedness<T>(ref IEnumerable<T> sequence, Func<T, T, int> comparer)
		{
			LazilyAssertWindowed2(ref sequence, (item1, item2) => comparer(item1, item2) <= 0);
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyAssertWindowed2<T>(ref IEnumerable<T> sequence, Func<T, T, bool> assertion)
		{
			T previous = default(T);
			bool first = true;
			sequence = sequence.Select(e =>
			{
				if (!first)
				{
					first = false;
					assertion(previous, e);
				}
				previous = e;
				return e;
			});
		}

		[DebuggerHidden, Conditional("DEBUG"), Pure]
		public static void RequiresEnumIsDefined<T>(T enumValue, params T[] otherAllowedValues) where T : struct
		{
			Requires(EnumIsDefined(enumValue) || otherAllowedValues.Contains(enumValue));
		}
		[DebuggerHidden]
		public static bool EnumIsDefined<T>(T enumValue) where T : struct
		{
			return Enum.IsDefined(typeof(T), enumValue);
		}
		/// <summary> Implementation detail of "LazilyAssertMinimalCount. </summary>
		private class CountAssertionEnumerator<T> : IEnumerator<T>
		{
			private readonly IEnumerator<T> underlyingEnumerable;
			/// <summary> A function that may throw an exception depening on the current count of elements in the underlying enumerable. </summary>
			private readonly Func<int, bool, bool> assertCount;

			private bool skipCountAssertionOnDisposal;

			private int count;
			/// <summary> </summary>
			/// <param name="underlyingEnumerable"></param>
			/// <param name="assertCount"> The return value is ignored, taken to be true regardless, if the second argument is true. </param>
			/// <param name="skipCountAssertionOnDisposal"></param>
			[DebuggerHidden]
			public CountAssertionEnumerator(IEnumerable<T> underlyingEnumerable, Func<int, bool, bool> assertCount, bool skipCountAssertionOnDisposal = false)
			{
				this.assertCount = assertCount;
				this.underlyingEnumerable = underlyingEnumerable.GetEnumerator();
				this.skipCountAssertionOnDisposal = skipCountAssertionOnDisposal;
			}
			[DebuggerHidden]
			public bool MoveNext()
			{
				if (!underlyingEnumerable.MoveNext())
				{
					assertCount(count, true);
					skipCountAssertionOnDisposal = true;
					return false;
				}
				count++;
				skipCountAssertionOnDisposal |= assertCount(count, false);
				return true;
			}

			public T Current
			{
				get
				{
					return underlyingEnumerable.Current;
				}
			}
			public void Reset()
			{
				underlyingEnumerable.Reset();
				count = 0;
			}
			[DebuggerHidden]
			public void Dispose()
			{
				if (skipCountAssertionOnDisposal)
				{
					while (MoveNext())
					{
						//intentionally empty. MoveNext may trigger count assertion exceptions
					}
				}
				underlyingEnumerable.Dispose();
			}
			object IEnumerator.Current
			{
				get { return Current; }
			}
			[DebuggerHidden]
			public static CountAssertionEnumerator<T> Minimum(IEnumerable<T> underlyingEnumerable, int minimumCount)
			{
				return new CountAssertionEnumerator<T>(underlyingEnumerable, (currentCount, definitive) =>
				{
					if (definitive && currentCount < minimumCount)
						ThrowMinimumNotAttainedException(minimumCount, currentCount);
					return currentCount >= minimumCount;//skips complete enumeration of the underlying enumerable on its disposal
				});
			}
			[DebuggerHidden]
			public static CountAssertionEnumerator<T> Maximum(IEnumerable<T> underlyingEnumerable, int maximumCount)
			{
				return new CountAssertionEnumerator<T>(underlyingEnumerable, (currentCount, definitive) =>
				{
					if (currentCount > maximumCount)
						ThrowMaximumExceededException(maximumCount);
					return false;
				});
			}
			[DebuggerHidden]
			public static CountAssertionEnumerator<T> Exactly(IEnumerable<T> underlyingEnumerable, int requiredCount)
			{
				return new CountAssertionEnumerator<T>(underlyingEnumerable, (currentCount, definitive) =>
				{
					if (currentCount > requiredCount)
						ThrowMinimumNotAttainedException(requiredCount, currentCount);
					if (definitive && currentCount < requiredCount)
						ThrowMaximumExceededException(requiredCount);
					return false;
				});
			}

			[DebuggerHidden]
			public static void ThrowMinimumNotAttainedException(int minimumCount, int actualCount)
			{
				throw new Exception(string.Format("Too few elements: only {0} out of {1}", actualCount, minimumCount));
			}

			public static void ThrowMaximumExceededException(int maximumCount, int? actualCount = null)
			{
				string actualCountString = actualCount.HasValue ? actualCount + " exceeded " : "";
				throw new Exception(string.Format("Too many elements: {1}{0}", maximumCount, actualCountString));
			}
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyRequire(ref IEnumerable sequence, Func<object, bool> predicate, string message = null)
		{
			IEnumerable<object> temp = sequence.Cast<object>();
			LazilyRequires(ref temp, predicate, message);
			sequence = temp;
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyRequires<T>(ref IEnumerable<T> sequence, Func<T, bool> predicate, string message = null)
		{
			LazilyRequires(ref sequence, (e, i) => predicate(e), message);
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyRequires<T>(ref IEnumerable<T> sequence, Func<T, int, bool> predicate, string message = null)
		{
			sequence = sequence.Select((element, i) =>
			{
				if (!predicate(element, i)) throw new ContractException(message ?? string.Format("Contract failed: predicate not matched by {0}", element));
				return element;
			});
		}
		/// <summary> Lazily asserts that all selected objects are equal. </summary>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyRequiresEquality<TSource, T>(ref IEnumerable<TSource> sequence, Func<TSource, T> selector, IEqualityComparer<T> equalityComparer = null, string message = null)
		{
			equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

			T previousSelected = default(T);
			bool first = true;
			int index = 0;

			var debug = sequence.ToList();
			var assertingSequence = sequence.Select(element =>
			{

				T selected = selector(element);
				if (!first && !equalityComparer.Equals(previousSelected, selected))
				{
					throw new ContractException(string.Format(message ?? "The selected object at index {0} didn't equal the precedencing selected elements. ", index));
				}

				//side effects:
				first = false;
				previousSelected = selected;
				index++;

				return element;
			});

			bool lazinessCanBeAvoided = sequence is IList || sequence is IReadOnlyCollection<T>;
			if (lazinessCanBeAvoided)
			{
				foreach (var element in assertingSequence)
				{
					//iterate over the sequence to assert equality
				}
			}
			else
			{
				sequence = assertingSequence;
			}
		}
		/// <summary> Lazily asserts that any element in the specified sequence matches the predicate. </summary>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void LazilyRequireAny<T>(ref IEnumerable<T> sequence, Func<T, bool> predicate, string message = null)
		{
			bool evaluateNonLazily = sequence is IList || sequence is IReadOnlyCollection<T>;

			bool anyElementMatchedPredicate = false;
			Func<T, T> predicateThatStoresResult = element =>
			{
				if (!anyElementMatchedPredicate)
					anyElementMatchedPredicate = predicate(element);
				return element;
			};

			if (evaluateNonLazily)
			{
				foreach (T element in sequence)
				{
					predicateThatStoresResult(element);
					if (anyElementMatchedPredicate)
						return;
				}
				Assert(false, message);
			}
			else
			{
				sequence = sequence.Select(predicateThatStoresResult)
								   .ContinueWith(() => Assert(anyElementMatchedPredicate, message));
			}
		}

		/// <summary> Asserts that the calling method is called from a method or type with the specified name.  </summary>
		/// <param name="callerName"> The name of the calling method or declaring type of the calling method. </param>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void AssertCaller(string callerName)
		{
			var caller = new StackFrame(2).GetMethod();
			Assert(caller.Name == callerName || caller.DeclaringType.Name == callerName);
		}

		public static Func<int, bool> InRange(int start, int count)
		{
			return i => start <= i && i < start + count;
		}
#nullable enable
		/// <summary>
		/// Asserts that the specified sequences are equality according to the default element equality comparer.
		/// </summary>
		[DebuggerHidden, Conditional("DEBUG")]
		public static void AssertSequenceEqual<T>(IEnumerable<T> sequence,
												  IEnumerable<T> expectedSequence)
		{
			AssertSequenceEqual(sequence, expectedSequence, EqualityComparer<T>.Default.Equals);
		}
		[DebuggerHidden, Conditional("DEBUG")]
		public static void AssertSequenceEqual<T>(IEnumerable<T> sequence,
												  IEnumerable<T> expectedSequence,
												  IEqualityComparer<T> equalityComparer)
		{
			Contract.Requires(equalityComparer != null);
			AssertSequenceEqual(sequence, expectedSequence, equalityComparer.Equals);
		}
        /// <summary>
        /// Asserts that the specified sequences are equality according to the specified element equality comparer.
        /// </summary>
        [DebuggerHidden, Conditional("DEBUG")]
        public static void AssertSequenceEqual<T>(IEnumerable<T> sequence,
                                                  IEnumerable<T> expectedSequence,
                                                  string? baseMessage = null)
        {
            AssertSequenceEqual<T, T>(sequence, expectedSequence, EqualityComparer<T>.Default.Equals, baseMessage);
		}

        /// <summary>
        /// Asserts that the specified sequences are equality according to the specified element equality comparer.
        /// </summary>
        [DebuggerHidden, Conditional("DEBUG")]
		public static void AssertSequenceEqual<T, U>(IEnumerable<T> sequence,
													 IEnumerable<U> expectedSequence,
													 Func<T, U, bool> equalityComparer,
													 string? baseMessage = null)
		{
			Contract.Requires(sequence != null);
			Contract.Requires(expectedSequence != null);
			Contract.Requires(equalityComparer != null);

			if (baseMessage is not null)
			{
				baseMessage.TrimEnd(' ', '.', ',');
				baseMessage += ". ";
			}

            int index = 0;
			using (IEnumerator<T> enumerator1 = sequence.GetEnumerator())
			using (IEnumerator<U> enumerator2 = expectedSequence.GetEnumerator())
			{
				while (enumerator1.MoveNext())
				{
					if (!enumerator2.MoveNext())
					{
						int expectedCount = index + 1;
						while (enumerator1.MoveNext())
							expectedCount++;

						throw new ContractException($"{baseMessage}The sequence has more elements than expected (received {index}/{expectedCount})");
					}

                    if (!equalityComparer(enumerator1.Current, enumerator2.Current))
					{
						throw new ContractException($"{baseMessage}The element at index {index} didn't match the expected element: received '{enumerator1.Current}', but expected '{enumerator2.Current}'");
					}
					index++;
				}
				if (enumerator2.MoveNext())
				{
					int expectedCount = index + 1;
					while (enumerator2.MoveNext())
						expectedCount++;

					throw new ContractException($"{baseMessage}The sequence has fewer elements than expected (received {index}/{expectedCount})");
				}
			}
		}

		/// <summary>
		/// Asserts that the specified sets are equal according to the default element equality comparer.
		/// This method eschews GetHashCode on the set elements.
		/// </summary>
		[DebuggerHidden]
		public static void AssertSetEqual<T>(IEnumerable<T> sequence,
											 IEnumerable<T> expectedSequence,
											 out (IReadOnlyCollection<T>? Sequence, IReadOnlyCollection<T>? Expected) remainder)
		{
			AssertSetEqual(sequence, expectedSequence, EqualityComparer<T>.Default.Equals, out remainder);
		}

		/// <summary>
		/// Asserts that the specified sets are equal according to the specified element equality comparer.
		/// This method eschews GetHashCode on the set elements.
		/// </summary>
		[DebuggerHidden]
		public static void AssertSetEqual<T, U>(IEnumerable<T> sequence,
												IEnumerable<U> expectedSequence,
												Func<T, U, bool> equals,
												out (IReadOnlyCollection<T>? Sequence, IReadOnlyCollection<U>? Expected) remainder)
		{

#if DEBUG
			var a = sequence.ToList();
			var b = expectedSequence.ToList();
			var originalA = a.ToArray();
			var originalB = b.ToArray();
			a.RemoveAll(t => b.Any((u, i) =>
			{
				if (equals(t, u))
				{
					b.RemoveAt(i);
					return true;
				}
				return false;
			}));
			if (a.Count != 0 || b.Count != 0)
			{
				var test = a.Count != 0 && b.Count != 0 && equals(a[0], b[0]);
				remainder = (a, b);
				throw new ContractException("The specified sets don't match, see the out parameter for more specific results. ");
			}
#endif
			remainder = (null, null);
		}
		[DebuggerHidden]
		public static void AssertReferenceEquals(object a, object b)
		{
			Contract.Assert(ReferenceEquals(a, b), "The reference equality contract was broken");
		}
	}


	public class ContractException : Exception
	{
		public ContractException(string message = "The contract was broken")
			: base(message)
		{ }
	}

	public sealed class DefaultSwitchCaseUnreachableException : ContractException
	{
		public DefaultSwitchCaseUnreachableException(string message = "Forgot to add case") : base(message) { }
	}

	public sealed class AppSettingNotFoundException : ContractException
	{
		public AppSettingNotFoundException(string? key = null) : base($"AppSetting key {(key == null ? "" : $"'{key}' ")} not found. ") { }
	}

}
