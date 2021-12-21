using JBSnorro.Collections;
using JBSnorro.Collections.Sorted;
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Geometry
{
	[DebuggerDisplay("{string.Format(\"{2}{0}, {1}{3}\", Start, End, StartInclusive ? '[' : '(', EndInclusive ? ']' : ')')}")]
	public struct Interval
	{
		public int Start { get; }
		public int End { get; }
		public bool StartInclusive { get; }
		public bool EndInclusive { get; }
		public bool IsEmpty
		{
			get { return Start == End && !StartInclusive; }
		}
		public int Length
		{
			get
			{
				int effectiveEnd = End + (EndInclusive ? 1 : 0);
				int effectiveStart = Start + (StartInclusive ? 0 : 1);
				return effectiveEnd - effectiveStart;
			}
		}

		[DebuggerHidden]
		public Interval(int start, int end, bool startInclusive = true, bool endInclusive = false) : this()
		{
			this.Start = start;
			this.End = end;
			this.StartInclusive = startInclusive;
			this.EndInclusive = endInclusive;

			AssertInvariants();
		}
		/// <summary> Creates a new interval out of a set of booleans indicating whether that position is contained in the interval. </summary>
		/// <param name="flags"> Booleans with at most one contiguous stretch of true's, representing the interval to create. </param>
		public Interval(IEnumerable<bool> flags)
		{
			Contract.Requires(flags != null);

			//make field definitively assigned
			Start = 0;



			bool trueFound = false;
			bool falseFoundAfterTrue = false;
			int i = 0;
			foreach (bool flag in flags)
			{
				if (flag)
				{
					if (!trueFound)
					{
						Start = i;
						trueFound = true;
					}
					Contract.Assert(!falseFoundAfterTrue, "Two intervals are specified");
				}
				else
				{
					if (trueFound)
					{
#if DEBUG
						falseFoundAfterTrue = true;
						continue;//skip increment in debug mode
#else
						break;
#endif
					}
				}
				i++;
			}

			if (trueFound)
			{
				End = i;
				StartInclusive = true;
				EndInclusive = false;
			}
			else
			{
				//trueFound can remain false if only falses were specified, in which case Start = End = 0
				End = 0;
				StartInclusive = false;
				EndInclusive = false;
			}

			AssertInvariants();
		}

		/// <summary> Gets whether this interval has at least one point overlap with the specified interval. </summary>
		public bool OverlapsWith(Interval other)
		{
			AssertInvariants();
			other.AssertInvariants();

			if (this.IsEmpty || other.IsEmpty)
				return false;

			if (Start < other.Start)
			{
				if (End > other.Start)
				{
					return true;
				}
				else if (End == other.Start && EndInclusive && other.StartInclusive)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else if (Start == other.Start)
			{
				return true;
			}
			else
			{
				return other.OverlapsWith(this);
			}
		}
		/// <summary> Gets whether this interval has no points overlap with the specified interval. </summary>
		public bool DisjointFrom(Interval other)
		{
			return !OverlapsWith(other);
		}
		/// <summary> Gets whether this interval contains all points in the specified interval. </summary>
		public bool Contains(Interval containee)
		{
			AssertInvariants();
			containee.AssertInvariants();

			if (this.IsEmpty)
			{
				return false;
			}

			if (Start > containee.Start)
			{
				//start stretch not included
				return false;
			}
			if (Start == containee.Start && !StartInclusive && containee.StartInclusive)
			{
				//starting point not included
				return false;
			}

			if (End < containee.End)
			{
				//end stretch not included
				return false;
			}

			if (End == containee.End && !EndInclusive && containee.EndInclusive)
			{
				//end point not included
				return false;
			}

			return true;
		}
		/// <summary> Gets whether this interval contains the specified interval and more. Note that _only_ the integers are considered, so that [0, 2) does NOT strictly contain [0, 1]. </summary>
		public bool StrictlyContains(Interval containee)
		{
			return Contains(containee) && this != containee;
		}

		/// <summary> Gets whether this interval has at least one point in common with the specified interval, and that both intervals have at least one point the other doesn't. </summary>
		public bool HasStrictlyPartialOverlapWith(Interval other)
		{
			return this.OverlapsWith(other) && !this.Contains(other) && !other.Contains(this);
		}
		/// <summary> Gets whether this interval contains the specified point. </summary>
		public bool Contains(int i)
		{
			if (Start > i)
			{
				return false;
			}
			if (Start == i && !StartInclusive)
			{
				return false;
			}

			if (End < i)
			{
				return false;
			}
			if (End == i && !EndInclusive)
			{
				return false;
			}

			return true;
		}
		public override bool Equals(object obj)
		{
			if (obj is Interval)
				return Equals((Interval)obj);
			return false;
		}
		public bool Equals(Interval other)
		{
			if (this.StartInclusive == other.StartInclusive)
			{
				if (this.Start != other.Start)
					return false;
			}
			else if (this.StartInclusive)
			{
				if (this.Start != other.Start + 1)
					return false;
			}
			else
			{
				if (this.Start + 1 != other.Start)
					return false;
			}

			if (this.EndInclusive == other.EndInclusive)
			{
				return this.End == other.End;
			}
			else if (this.EndInclusive)
			{
				return this.End + 1 == other.End;
			}
			else
			{
				return this.End == other.End + 1;
			}
		}
		public static bool operator ==(Interval a, Interval b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(Interval a, Interval b)
		{
			return !(a == b);
		}
		public override int GetHashCode()
		{
			throw new NotImplementedException();
		}

		[InvariantMethod, DebuggerHidden]
		public void AssertInvariants()
		{
			Contract.Invariant(Start <= End);
			Contract.Invariant(Start != End || StartInclusive == EndInclusive, "An interval of one point must specify consistently whether it contains that point");
			Contract.Invariant(Start != End || StartInclusive || Start == 0, "Any empty interval must be situated at 0. ");
		}
	}

	public static class IntervalExtensions
	{
		/// <summary> Compares two non-empty intervals by their starting position. </summary>
		public static int Compare(Interval a, Interval b)
		{
			Contract.Requires(!a.IsEmpty);
			Contract.Requires(!b.IsEmpty);

			return a.Start.CompareTo(b.Start);
		}
		/// <summary> Returns whether the specified intervals are disjoint. </summary>
		/// <param name="intervals"> The intervals to inspect, ordered by interval start. </param>
		/// <returns> an empty specified enumerable is considered disjoint. </returns>
		public static bool AreDisjoint(this ISortedEnumerable<Interval> intervals)
		{
			Contract.Requires(intervals != null);
			Contract.RequiresForAll(intervals, interval => !interval.IsEmpty);
			Contract.Requires(intervals.IsSorted(comparer: Compare));

			Interval previous = default(Interval);
			bool first = true;
			foreach (Interval interval in intervals)
			{
				if (first)
				{

					first = false;
				}
				else
				{
					if (!previous.DisjointFrom(interval))
					{
						return false;
					}
				}
				previous = interval;
			}

			return true;
		}
		/// <summary> Creates a bit array with minimal length with the bits in the specified interval set.  </summary>
		public static BitArray ToBitArray(this Interval interval)
		{
			return interval.ToBitArray(interval.End);
		}
		/// <summary> Creates a bit array with specified length with the bits in the specified interval set.  </summary>
		public static BitArray ToBitArray(this Interval interval, int length)
		{
			interval.AssertInvariants();
			Contract.Requires(interval.Start >= 0);
			Contract.Requires<NotImplementedException>(interval.StartInclusive);
			Contract.Requires<NotImplementedException>(!interval.EndInclusive);

			return new BitArray(Enumerable.Range(interval.Start, interval.End - interval.Start), length);
		}
		/// <summary> Creates an immutable bit array with minimal length with the bits in the specified interval set.  </summary>
		public static ImmutableBitArray ToImmutableBitArray(this Interval interval)
		{
			return new ImmutableBitArray(interval.ToBitArray());
		}
		/// <summary> Creates an immutable bit array with specified length with the bits in the specified interval set.  </summary>
		public static ImmutableBitArray ToImmutableBitArray(this Interval interval, int length)
		{
			return new ImmutableBitArray(interval.ToBitArray(length));
		}
		/// <summary> Iterates over the integers in the interval. </summary>
		public static IEnumerable<int> AsEnumerable(this Interval interval)
		{
			int start = interval.StartInclusive ? interval.Start : (interval.Start + 1);
			int endExclusive = interval.EndInclusive ? interval.End + 1 : interval.End;
			return Enumerable.Range(start, endExclusive - start);
		}
	}

	/// <summary> Compares two non-empty intervals by their starting position, regardless of overlap. </summary>
	public sealed class IntervalComparerByStart : IComparer<Interval>
	{
		/// <summary> Compares two non-empty intervals by their starting position, regardless of overlap. </summary>
		public static IntervalComparerByStart Instance { get; } = new IntervalComparerByStart();

		int IComparer<Interval>.Compare(Interval x, Interval y)
		{
			x.AssertInvariants();
			y.AssertInvariants();

			Contract.Requires(!x.IsEmpty, "Empty intervals cannot be ordered");
			Contract.Requires(!y.IsEmpty, "Empty intervals cannot be ordered");

			return x.Start.CompareTo(y.Start);
		}
	}

	/// <summary> Compares two non-empty non-overlapping intervals by their starting position, regardless of overlap. </summary>
	public sealed class DisjointIntervalComparerByStart : IComparer<Interval>
	{
		/// <summary> Compares two non-empty non-overlapping intervals by their starting position, regardless of overlap. </summary>
		public static DisjointIntervalComparerByStart Instance { get; } = new DisjointIntervalComparerByStart();

		int IComparer<Interval>.Compare(Interval x, Interval y)
		{
			x.AssertInvariants();
			y.AssertInvariants();

			Contract.Requires(!x.IsEmpty, "Empty intervals cannot be ordered");
			Contract.Requires(!y.IsEmpty, "Empty intervals cannot be ordered");
			Contract.Requires(x.DisjointFrom(y), "Overlapping intervals cannot be compared");

			return x.Start.CompareTo(y.Start);
		}
	}

}
