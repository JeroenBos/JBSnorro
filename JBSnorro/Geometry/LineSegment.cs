using JBSnorro.SystemTypes;
using System;

namespace JBSnorro.Geometry
{
	class LineSegment : Line
	{
		public Point A { get; private set; }
		public Point B { get; private set; }

		public LineSegment(Point a, Point b)
			: base(a, b)
		{
			this.A = a;
			this.B = b;
		}

		/// <summary> Determines whether the current line segment intersects the specified line. </summary>
		public override bool Intersects(Line other)
		{
			if (!(other is LineSegment)) return other.Intersects(this);//this simpler case is handled by the base class

			Point intersection = FindExtrapolatedIntersection(this, other);
			if (double.IsNaN(intersection.X)) return false;//the lines are parallel 

			//the intersection may lie on the extrapolation of the segments. To check that they are within the boundaries, the contains method is invoked for each segment
			return this.Contains(intersection) && other.Contains(intersection);
		}


		/// <summary> Returns whether this line segment contains the specified point. </summary>
		public override bool Contains(Point point)
		{
			return base.Contains(point) //the base case returns whether the point lies on (the extrapolation of) this line segment 
				&& EqualityExtensions.ApproximatelyInInterval(point.X, this.A.X, this.B.X) // point.X ∈ (-ε + min(A.X, B.X), max(A.X, B.X) + ε)
				&& EqualityExtensions.ApproximatelyInInterval(point.X, this.A.X, this.B.X); // point.Y ∈ (-ε + min(A.Y, B.Y), max(A.Y, B.Y) + ε)
		}

		/// <summary> Determines whether the specified object is equal to the current. </summary>
		public override bool Equals(object? obj)
		{
			if (obj is LineSegment)
				return this.Equals((LineSegment)obj);
			else
				return base.Equals(obj);
		}
		/// <summary> Determines whether the specified line is equal to the current. </summary>
		public override bool Equals(Line other)
		{
			if (other is LineSegment)
				return this.Equals((LineSegment)other);
			return false;
		}
		public override int GetHashCode()
		{
			// to prevent warning
			throw new InvalidOperationException();
		}
		/// <summary> Determines whether the specified line segment is equal to the current. </summary>
		public bool Equals(LineSegment other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return this.A.ApproximatelyEquals(other.A) && this.B.ApproximatelyEquals(other.B);
		}
	}
}
