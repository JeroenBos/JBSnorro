using JBSnorro.Diagnostics;
using JBSnorro.SystemTypes;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace JBSnorro
{
	public static class FloatingTypeEqualityComparisonHelper
	{
		// ReSharper disable ParameterHidesMember
		private const double tolerance = 0.0001f;
		/// <summary> Determines whether the two specified floating point integers are equal (up to an optionally specified tolerance). </summary>
		public static bool EqualByTolerance(this double a, double b, double tolerance = tolerance)
		{
			return Math.Abs(a - b) < tolerance;
		}
		/// <summary> Determines whether the coordinates of the two specified points are equal (up to an optionally specified tolerance). </summary>
		public static bool EqualByTolerance(this Point a, Point b, double tolerance = tolerance)
		{
			return EqualByTolerance(a.X, b.X, tolerance) && EqualByTolerance(a.Y, b.Y, tolerance);
		}
		/// <summary> Determines whether the specified value is in the specified interval, or sufficiently close to it. </summary>
		/// <param name="value"> The value to be checked that it is in the specified interval. </param>
		/// <param name="boundary1"> One boundary of the interval. </param>
		/// <param name="boundary2"> Another boundary of the interval. </param>
		/// <param name="tolerance"> The maximum distance for a value to the interval, such that the value considered close enough to the interval to be contained in it. </param>
		public static bool InToleranceInterval(this double value, double boundary1, double boundary2, double tolerance = tolerance)
		{
			double min = Math.Min(boundary1, boundary2);
			double max = Math.Max(boundary1, boundary2);

			return min - tolerance < value && value < max + tolerance;
		}
	}
}
