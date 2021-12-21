using JBSnorro.Diagnostics;
using JBSnorro.SystemTypes;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JBSnorro.Geometry
{
	public static class RectangleExtensions
	{
		/// <summary> Returns the 4 corners of the specified Rect. </summary>
		public static IEnumerable<Point> GetCorners(this Rect Rect)
		{
			yield return new Point(Rect.Left, Rect.Top);
			yield return new Point(Rect.Right, Rect.Top);
			yield return new Point(Rect.Right, Rect.Bottom);
			yield return new Point(Rect.Left, Rect.Bottom);
		}

		/// <summary> Returns the parts of a not overlapping with b. </summary>
		[DebuggerHidden]
		public static Rect[] Subtract(Rect a, Rect b)
		{
			return Subtract(a, ref b);
		}
		/// <summary> Returns the parts of a not overlapping with b. </summary>
		public static Rect[] Subtract(Rect a, ref Rect b)
		{
			var legs = a.GetCorners().Count(b.Contains);
			if (legs == 0)
			{
				return a.Substract0Legged(ref b);
			}
			else if (legs == 1)
			{
				return a.Substract1Legged(b);
			}
			else if (legs == 2)
			{
				return a.Substract2Legged(b);
			}
			else
			{
				Contract.Assert(legs == 4);
				return EmptyCollection<Rect>.Array; //rect is already contained in r
			}
		}
		/// <summary> Returns the part of Rect a that is not contained in b, assuming that exactly 2 corners of a are in b. </summary>
		public static Rect[] Substract2Legged(this Rect a, Rect b)
		{
			bool rightQ = b.Left <= a.Right && a.Right <= b.Right; //a.right is in b
			bool leftQ = b.Left <= a.Left && a.Left <= b.Right; //a.left is in b
			bool topQ = b.Top <= a.Top && a.Top <= b.Bottom; //a.top is in b
			bool bottomQ = b.Top <= a.Bottom && a.Bottom <= b.Bottom; //a.bottom is in b

			Contract.Assert(new[] { rightQ, leftQ, topQ, bottomQ }.Count(_ => _) == 3, "2 corners of a must be in b");

			double right = a.Right;
			double left = a.Left;
			double top = a.Top;
			double bottom = a.Bottom;

			if (!rightQ)
				left = b.Right;
			if (!leftQ)
				right = b.Left;
			if (!topQ)
				bottom = b.Top;
			if (!bottomQ)
				top = b.Bottom;

			return new[] { FromExplicitCoordinates(left, right, top, bottom) };
		}
		/// <summary> Returns the two parts of Rect a that are not contained in b, assuming that exactly 1 corner of a is in b. </summary>
		public static Rect[] Substract1Legged(this Rect a, Rect b)
		{
			bool rightQ = b.Left <= a.Right && a.Right <= b.Right; //a.right is in b
			bool leftQ = b.Left <= a.Left && a.Left <= b.Right; //a.left is in b
			bool topQ = b.Top <= a.Top && a.Top <= b.Bottom; //a.top is in b
			bool bottomQ = b.Top <= a.Bottom && a.Bottom <= b.Bottom; //a.bottom is in b
																	  //note on equality signs: they've been chosen in accordance with a.o. Rect.Contains and Rect.IntersectsWith. 
																	  //In the worst case a few rectangles with zero width or height are created, which trivially aren't added anyway
																	  //hence there is hardly any overhead for simply continuing with conventions set by the framework

			Contract.Assert(new[] { rightQ, leftQ, topQ, bottomQ }.Count(_ => _) == 2, "1 corner of a must be in b");

			Rect result1, result2;
			if (leftQ && topQ)
			{
				//a.TopLeftCorner is in b
				result1 = FromExplicitCoordinates(b.Right, a.Right, a.Top, b.Bottom);
				result2 = FromExplicitCoordinates(a.Left, a.Right, b.Bottom, a.Bottom);
			}
			else if (rightQ && topQ)
			{
				result1 = FromExplicitCoordinates(a.Left, b.Left, a.Top, b.Bottom);
				result2 = FromExplicitCoordinates(a.Left, a.Right, b.Bottom, a.Bottom);
			}
			else if (rightQ && bottomQ)
			{
				result1 = FromExplicitCoordinates(a.Left, a.Right, a.Top, b.Top);
				result2 = FromExplicitCoordinates(a.Left, b.Left, b.Top, a.Bottom);
			}
			else
			{
				Contract.Assert(leftQ && bottomQ);
				result1 = FromExplicitCoordinates(a.Left, a.Right, a.Top, b.Top);
				result2 = FromExplicitCoordinates(b.Right, a.Right, b.Top, a.Bottom);
			}
			return new[] { result1, result2 };
		}
		/// <summary> Returns parts of a that are not contained in b, assuming that no corner of a is in b. </summary>
		public static Rect[] Substract0Legged(this Rect a, ref Rect b)
		{
			bool rightQ = b.Left <= a.Right && a.Right < b.Right; //a.right is in b
			bool leftQ = b.Left <= a.Left && a.Left < b.Right; //a.left is in b
			bool topQ = b.Top <= a.Top && a.Top < b.Bottom; //a.top is in b
			bool bottomQ = b.Top <= a.Bottom && a.Bottom < b.Bottom; //a.bottom is in b

			//no corner of a may be in b
			Contract.Assert(!(leftQ && topQ));
			Contract.Assert(!(rightQ && topQ));
			Contract.Assert(!(rightQ && bottomQ));
			Contract.Assert(!(leftQ && bottomQ));

			int invCornerCount = b.GetCorners().Count(a.Contains);
			if (invCornerCount == 2)
			{
				Rect[] result = Substract2Legged(b, a);
				b = a; //must be set after calling Substract2Legged
				return result;
			}
			else if (invCornerCount == 4)
			{
				b = default(Rect);
				return new[] { a };
			}
			if (leftQ && rightQ)
			{
				//b is horizontal
				if (b.Bottom <= a.Top || b.Top >= a.Bottom)
				{
					//no intersection
				}
				else
				{
					//top part of a:
					Rect result1 = FromExplicitCoordinates(a.Left, a.Right, a.Top, b.Top);
					//bottom part of a:
					Rect result2 = FromExplicitCoordinates(a.Left, a.Right, b.Bottom, a.Bottom);
					return new[] { result1, result2 };
				}
			}
			else if (topQ && bottomQ)
			{
				//b is vertical
				if (b.Left >= a.Right || b.Right <= a.Left)
				{
					//no intersection
				}
				else
				{
					//left part of a:
					Rect result1 = FromExplicitCoordinates(a.Left, b.Left, a.Top, a.Bottom);
					//right part of b:
					Rect result2 = FromExplicitCoordinates(b.Right, a.Right, a.Top, a.Bottom);
					return new[] { result1, result2 };
				}
			}

			//no intersection
			return new[] { a };
		}

		/// <summary> Creates a Rect from the coordinates right, left, top and bottom. </summary>
		public static Rect FromExplicitCoordinates(double left, double right, double top, double bottom)
		{
			Contract.Requires(left <= right);
			Contract.Requires(top <= bottom);
			return new Rect(left, top, right - left, bottom - top);
		}


	}
}
