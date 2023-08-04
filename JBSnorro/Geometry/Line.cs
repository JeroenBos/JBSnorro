using JBSnorro.Diagnostics;
using JBSnorro.SystemTypes;
using System;

namespace JBSnorro.Geometry
{
	class Line
	{
		public double LinearCoefficient { get; private set; }
		public double LinearAddend { get; private set; }

		/// <summary> Gets whether this line is vertical. </summary>
		public bool IsVertical
		{
			get { return EqualityExtensions.ApproximatelyEquals(0, 1 / this.LinearCoefficient); }
		}
		/// <summary> Gets whether this line is horizontal. </summary>
		public bool IsHorizontal
		{
			get { return EqualityExtensions.ApproximatelyEquals(0, this.LinearCoefficient); }
		}
		/*
				/// <summary> Returns whether the specified p is to the right of this line (or its extrapolation), from the perspective of one following this line. </summary>
				public virtual bool IsToTheRight(PointF p)
				{
					if(this.IsVertical)
						if (LinearCoefficient < 0)
							return p.X < 0;
						else
							return p.X > 0;

					if (this.IsHorizontal)
					{
						if (LinearCoefficient < 0)
							return p.Y < 0;
						else
							return p.Y > 0;
					}
				}
				*/
		/// <summary> Gets the y-value of this line (or its extrapolation) for a given x value. </summary>
		public double GetYValueAt(double x)
		{
			if (IsVertical) throw new InvalidOperationException("Line is vertical");
			return this.LinearAddend + this.LinearCoefficient * x;
		}
		/// <summary> Gets the x-value of this line (or its extrapolation) for a given y value. </summary>
		public double GetXValueAt(double y)
		{
			if (IsVertical) throw new InvalidOperationException("Line is horizontal");
			return (y - this.LinearAddend) / this.LinearCoefficient;
		}
		/// <summary> Gets whether the specified line is parallel or antiparallel to this line. </summary>
		public bool IsParallelTo(Line other)
		{
			return EqualityExtensions.ApproximatelyEquals(Math.Abs(other.LinearCoefficient), Math.Abs(this.LinearCoefficient));
		}

		/// <summary> Determines whether the current line intersects the specified line. </summary>
		public virtual bool Intersects(Line line)
		{
			Point intersection = FindExtrapolatedIntersection(this, line);
			if (double.IsNaN(intersection.X))
				return false;//they're parallel

			//the intersection may be on the extrapolation of the specified line, and is checked by the Contains method:
			return line.Contains(intersection);
		}

		/// <summary> Returns whether this line contains the specified point. </summary>
		public virtual bool Contains(Point point)
		{
			return EqualityExtensions.ApproximatelyEquals(LinearAddend + LinearCoefficient * point.X, point.Y);
		}

		/// <summary> Creates a new infinitely extending line intersecting the two specified points. </summary>
		public Line(Point a, Point b) : this(GetLinearCoefficientAndAddend(a, b)) { }
		/// <summary> Creates a new infinitely extending line from a linear coefficient and addend. </summary>
		public Line(double linearCoefficient, double linearAddend)
		{
			this.LinearCoefficient = linearCoefficient;
			this.LinearAddend = linearAddend;
		}
		/// <summary> A helper method to bridge efficiently the other two ctors. </summary>
		private Line(Tuple<double, double> coefficientAndAddend) : this(coefficientAndAddend.Item1, coefficientAndAddend.Item2) { }
		/// <summary> Gets the linear coefficient and linear addend, respectively, for a line intersecting the two specified points. </summary>
		private static Tuple<double, double> GetLinearCoefficientAndAddend(Point a, Point b)
		{
			double linearCoefficient, linearAddend;
			if (EqualityExtensions.ApproximatelyEquals(a.X, b.X))
			{
				linearCoefficient = a.Y < b.Y ? float.NegativeInfinity : float.PositiveInfinity;
				linearAddend = float.NaN;
			}
			else
			{

				linearCoefficient = (b.Y - a.Y) / (b.X - a.X);
				linearAddend = a.Y - a.X * linearCoefficient;
			}
			return new Tuple<double, double>(linearCoefficient, linearAddend);
		}

		/// <summary> Determines whether the specified object is equal to the current. </summary>
		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is Line && Equals((Line)obj);
		}
		/// <summary> Determines whether the specified line is equal to the current. </summary>
		public virtual bool Equals(Line other)
		{
			return EqualityExtensions.ApproximatelyEquals(this.LinearCoefficient, other.LinearCoefficient)
				&& EqualityExtensions.ApproximatelyEquals(this.LinearAddend, other.LinearAddend);
		}
		/// <summary> Gets the hash code for this line. </summary>
		public override int GetHashCode()
		{
			throw new InvalidOperationException("This type does not have corresponding hash code");
		}



		/// <summary> Calculates the point of intersection. Returns NaN when the specified lines are parallel. </summary>
		protected static Point FindExtrapolatedIntersection(Line line1, Line line2)
		{
			if (line1.IsParallelTo(line2))
				return new Point(float.NaN, float.NaN);

			var x = (line2.LinearAddend - line1.LinearAddend) / (line1.LinearCoefficient - line2.LinearCoefficient);
			Contract.Assert(EqualityExtensions.ApproximatelyEquals(line1.GetYValueAt(x), line2.GetYValueAt(x)), "bug in line above");
			return new Point(x, line1.GetYValueAt(x));
		}
	}
}
