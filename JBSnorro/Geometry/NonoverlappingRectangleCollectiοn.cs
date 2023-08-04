using JBSnorro.Diagnostics;
using JBSnorro.SystemTypes;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JBSnorro.Geometry;

/// <summary> Represents a collection of non-overlapping rectangles. </summary>
public sealed class NonoverlappingRectangleCollectiοn : IEnumerable<Rect>
{
	private static readonly NonoverlappingRectangleCollectiοn empty = new NonoverlappingRectangleCollectiοn();
	/// <summary> Gets an empty nonoverlapping rectangle collection. </summary>
	public static NonoverlappingRectangleCollectiοn Empty
	{
		get { return empty; }
	}

	
	private List<Rect> rectangles;
	/// <summary> Gets or sets the rectangle at the specified index in the backing field. </summary>
	private Rect this[int index]
	{
		get { return this.rectangles[index]; }
		set { this.rectangles[index] = value; }
	}
	/// <summary> Gets the number of rectangles in this rectangle collection. </summary>
	private int Count
	{
		get { return this.rectangles.Count; }
	}

	/// <summary> Gets the height of the smallest encapsulating rectangle, i.e. the distance between the most upper and lowest points contained in this rectangle collection. </summary>
	public double Height
	{
		get
		{
			if (this.Count == 0)
				return 0;

			double minTop = double.MaxValue;
			double maxBottom = double.MinValue;
			foreach (var rect in rectangles)
			{
				minTop = Math.Min(minTop, rect.Top);
				maxBottom = Math.Max(maxBottom, rect.Bottom);
			}
			return maxBottom - minTop;
		}
	}
	/// <summary> Gets the width of the smallest encapsulating rectangle, i.e. the distance between the most left and most right points contained in this rectangle collection. </summary>
	public double Width
	{
		get
		{
			if (this.Count == 0)
				return 0;

			double minLeft = double.MaxValue;
			double maxRight = double.MinValue;
			foreach (var rect in rectangles)
			{
				minLeft = Math.Min(minLeft, rect.Left);
				maxRight = Math.Max(maxRight, rect.Right);
			}
			return maxRight - minLeft;
		}
	}
	/// <summary> Gets the most right coordinate contained by this rectangle collection. </summary>
	public double Right
	{
		get
		{
			if (rectangles.Count == 0)
				return 0;

			double result = double.MinValue;
			foreach (Rect r in this.rectangles)
				result = Math.Max(result, r.Right);
			return result;
		}
	}
	/// <summary> Gets the most left coordinate contained by this rectangle collection. </summary>
	public double Left
	{
		get
		{
			if (this.rectangles.Count == 0)
				return 0;
			double result = double.MaxValue;
			foreach (Rect r in this.rectangles)
				result = Math.Min(result, r.Left);
			return result;
		}
	}
	/// <summary> Gets the bottom most coordinate contained by this rectangle collection. </summary>
	public double Bottom
	{
		get
		{
			if (this.rectangles.Count == 0)
				return 0;
			double result = double.MinValue;
			foreach (Rect r in this.rectangles)
				result = Math.Max(result, r.Bottom);
			return result;
		}
	}
	/// <summary> Gets the top most coordinate contained by this rectangle collection. </summary>
	public double Top
	{
		get
		{
			if (this.rectangles.Count == 0)
				return 0;
			double result = double.MaxValue;
			foreach (Rect r in this.rectangles)
				result = Math.Min(result, r.Top);
			return result;
		}
	}
	/// <summary> Gets the bottom right point of the smallest encapsulating rectangle. </summary>
	public Point BottomRight
	{
		get { return new Point(Right, Bottom); }
	}
	/// <summary> Gets the top right point of the smallest encapsulating rectangle. </summary>
	public Point TopRight
	{
		get { return new Point(Right, Top); }
	}
	/// <summary> Gets the bottom left point of the smallest encapsulating rectangle. </summary>
	public Point BottomLeft
	{
		get { return new Point(Left, Bottom); }
	}
	/// <summary> Gets the top left point of the smallest encapsulating rectangle. </summary>
	public Point TopLeft
	{
		get { return new Point(Left, Top); }
	}
	/// <summary> Gets the smallest encapsulating rectangle. </summary>
	public Rect Enclosement
	{
		get { return new Rect(Left, Right, Width, Height); }
	}
	/// <summary> Gets the smallest size of the smallest encapsulating rectangle. </summary>
	public Size Size
	{
		get { return new Size(this.Width, this.Height); }
	}

	/// <summary> Creates a new empty collection of rectangles. </summary>
	public NonoverlappingRectangleCollectiοn()
	{
		rectangles = new List<Rect>();
	}
	/// <summary> Creates a rectangular polygon from the specified rectangle. </summary>
	/// <param name="constituent"> The rectangle that define the area of this polygon. </param>
	public NonoverlappingRectangleCollectiοn(Rect constituent) : this(constituent.ToSingleton()) { }
	/// <summary> Creates a rectangular polygon from the specified rectangles. </summary>
	/// <param name="constituents"> The rectangles that define the area of this polygon. May overlap. May not be null or empty. </param>
	public NonoverlappingRectangleCollectiοn(IEnumerable<Rect> constituents)
	{
		Contract.Requires(constituents != null);

		this.rectangles = new List<Rect>();
		foreach (Rect constituent in constituents)
			this.Add(constituent);
	}
	/// <summary> Clones the specified nonoverlapping rectangle collection. </summary>
	/// <param name="clone"> The collection to clone. </param>
	public NonoverlappingRectangleCollectiοn(NonoverlappingRectangleCollectiοn clone) : this(clone.ToSingleton()) { }
	/// <summary> Creates a new nonoverlapping rectangle collection containing all specified rectangles. </summary>
	/// <param name="compositeConstituents"> The rectangle collections to encorporate in this collection. </param>
	public NonoverlappingRectangleCollectiοn(IEnumerable<NonoverlappingRectangleCollectiοn> compositeConstituents)
		: this()
	{
		foreach (var compositeConstituent in compositeConstituents)
			this.rectangles.AddRange(compositeConstituent);
	}
	/// <summary> Creates a new nonoverlapping rectangle collection containing all specified rectangles. </summary>
	/// <param name="compositeConstituents"> The rectangle collections to encorporate in this collection. </param>
	public NonoverlappingRectangleCollectiοn(params NonoverlappingRectangleCollectiοn[] compositeConstituents) : this((IEnumerable<NonoverlappingRectangleCollectiοn>)compositeConstituents) { }

	/// <summary> Returns whether the specified point lies in this polygon. </summary>
	/// <param name="point"> The point ot be checked whether it is in this polygon. </param>
	/// <param name="offset"> The offset of this polygon. </param>
	public bool Contains(Point point, Point offset = default(Point))
	{
		point.X -= offset.X;
		point.Y -= offset.Y;
		return this.Any(element => element.Contains(point));
	}
	/// <summary> Returns whether this polygon intersects with the specified rectangle. </summary>
	/// <param name="rectangle"> The rectangle to check whether it intersects with this polygon. </param>
	public bool IntersectsWith(Rect rectangle)
	{
		return this.Any(element => element.IntersectsWith(rectangle));
	}

	/// <summary> Returns a new polygon with the area of the specified rectangle added to its area. </summary>
	/// <param name="rect"> The rectangle to be added to this polygon. May overlap. </param>
	/// <param name="offset"> An optional offset to add the rectangle at. </param>
	public void Add(Rect rect, Point offset = default(Point))
	{
		Contract.Requires(!double.IsNaN(rect.Bottom));
		Contract.Requires(!double.IsNaN(rect.Top));
		Contract.Requires(!double.IsNaN(rect.Left));
		Contract.Requires(!double.IsNaN(rect.Right));

		if (!rect.IsEmpty)
		{
			this.Add(new Rect(rect.X + offset.X, rect.Y + offset.Y, rect.Width, rect.Height), 0);
			this.Simplify();
		}
	}
	/// <summary> A submethod of Add. Adds the specified rectangle to this polygon, skipping overlap checking with the first i elements of this polygon. </summary>
	/// <param name="rect"> The rectangle to add to this collection. </param>
	/// <param name="i"> The number of rectangles in this collection with which the specified rectangle is guaranteed not overlap with. </param>
	private void Add(Rect rect, int i)
	{
		if (rect.Height == 0 || rect.Width == 0)
			return;

		Contract.Assert(i == 0 || this.rectangles.Take(i - 1).All(element =>
		{
			bool intersectsIncludingBoundaries = element.IntersectsWith(rect);
			if (!intersectsIncludingBoundaries)
				return true;

			var original = element;//else check if boundaries intersect (which don't count as intersections)
			element.Intersect(rect);
			return element.Width == 0 || element.Height == 0 || element == Rect.Empty;
		}), "The specified rectangle may not intersect with the first i elements");

		for (; i < this.rectangles.Count; i++)
		{
			Rect elementAti = this[i];
			Rect[] remainder = RectangleExtensions.Subtract(rect, ref elementAti);
			this[i] = elementAti;

			if (remainder.Length == 1 && remainder[0] == rect) //if no part of rect was subtracted
				continue;                                      //then continue with the next rectangle in this collection


			//otherwise a part of rect was subtracted and isn't contained in the remainder any more
			//that means the remainder doesn't intersect with the first i + 1 rectangles in this collection and the remainder is added accordingly
			for (int j = 0; j < remainder.Length; j++)
				this.Add(remainder[j], i + 1);              //is correct even considering the swap this[i] = elementAti
			return;
		}

		//this can only be reached if the continue statement was reached for every rectangle in this collection. 
		//That implies rect does not intersect with any rectangle in this collection and can therefore simply be added
		this.rectangles.Add(rect);
	}
	/// <summary> Returns a new polygon with the area of the specified polygon added to its area. </summary>
	/// /// <param name="polygon"> The polygon to be added to this polygon. May overlap. </param>
	public void Add(NonoverlappingRectangleCollectiοn polygon, Point offset = default(Point))
	{
		polygon.CheckInvariants();
		foreach (var rectangle in polygon)
			this.Add(rectangle, offset);

		this.Simplify();
	}

	/// <summary> Returns a new polygon with the area of the specified polygon added to its area. </summary>
	/// /// <param name="polygon"> The polygon to be added to this polygon. May overlap. </param>
	/// <param name="xOffset"> The offset in the horizontal direction where the polygon is to be added. </param>
	/// <param name="yOffset"> The offset in the vertical direction where the polygon is to be added. </param>
	[DebuggerHidden]
	public void Add(NonoverlappingRectangleCollectiοn polygon, double xOffset, double yOffset)
	{
		this.Add(polygon, new Point(xOffset, yOffset));
	}

	private void Simplify()
	{
		//removes all empty rectangles
		for (int i = 0; i < this.Count; i++)
			if (this[i].Width == 0 || this[i].Height == 0)
			{
				this[i] = this.rectangles.Last();
				this.rectangles.RemoveLast();
			}
	}
	/// <summary> Adds the specified polygon adjacently to the right of the current polygon, where the top of the specified polygon will be identified with the line y = 0 in the reference frame of this polygon. </summary>
	/// <param name="polygon"> The polygon to place. </param>
	/// <param name="offset"> The offset to add to the specified polygon. </param>
	[DebuggerHidden]
	public void AddRight(NonoverlappingRectangleCollectiοn polygon, Point offset = default(Point))
	{
		double dummy;
		AddRight(polygon, out dummy, offset);
	}
	/// <summary> Adds the specified polygon adjacently to the right of the current polygon, where the top of the specified polygon will be identified with the line y = 0 in the reference frame of this polygon. </summary>
	/// <param name="polygon"> The polygon to place. </param>
	/// <param name="placementRight"> The x coordinate where the specified polygon was placed. </param>
	/// /// <param name="offset"> The offset to add to the specified polygon. The polygon is placed at least at the specified x coordinate. </param>
	public void AddRight(NonoverlappingRectangleCollectiοn polygon, out double placementRight, Point offset = default(Point))
	{
		placementRight = GetHorizontalOverlap(polygon, offset);
		this.Add(polygon, new Point(offset.X + placementRight, offset.Y));
	}
	/// <summary> Gets the hortizontal coordinate where the specified could be added to the current polygon without overlap. </summary>
	/// <param name="polygon"> The polygon to be added to this </param>
	/// <param name="offset"> The minimum x coordinate where the polygon could be placed. The y coordinate is an ordinary offset. </param>
	private double GetHorizontalOverlap(NonoverlappingRectangleCollectiοn polygon, Point offset = default(Point))
	{
		Contract.Requires(polygon != null);

		double largestHorizontalOverlap = 0;
		foreach (Rect r in polygon)
			foreach (Rect element in this)
			{
				double horizontalOverlap = element.Right - (r.X + offset.X);
				if (horizontalOverlap > largestHorizontalOverlap)
					largestHorizontalOverlap = horizontalOverlap;
			}
		return largestHorizontalOverlap;
	}
	/// <summary> Adds the specified polygon adjacently below the current polygon, where the left of the specified polygon will be identified with the line x = 0 in the reference frame of this polygon. </summary>
	/// <param name="polygon"> The polygon to place. </param>
	/// <param name="offset"> The offset to add to the specified polygon. The polygon is placed at least at the specified y coordinate. </param>
	[DebuggerHidden]
	public void AddBottom(NonoverlappingRectangleCollectiοn polygon, Point offset = default(Point))
	{
		double dummy;
		AddBottom(polygon, out dummy, offset);
	}
	/// <summary> Adds the specified polygon adjacently below the current polygon, where the left of the specified polygon will be identified with the line x = 0 in the reference frame of this polygon. </summary>
	/// <param name="polygon"> The polygon to place. </param>
	/// <param name="placementBottom"> The y coordinate where the specified polygon was placed. </param>
	/// <param name="offset"> The offset to add to the specified polygon. The polygon is placed at least at the specified y coordinate. </param>
	public void AddBottom(NonoverlappingRectangleCollectiοn polygon, out double placementBottom, Point offset = default(Point))
	{
		placementBottom = GetVerticalOverlap(polygon, offset);
		this.Add(polygon, new Point(offset.X, offset.Y + placementBottom));
	}
	/// <summary> Gets the vertical coordinate where the specified could be added to the current polygon without overlap. </summary>
	/// <param name="polygon"> The polygon to be added to this </param>
	/// <param name="offset"> The minimum y coordinate where the polygon could be placed. The x coordinate is an ordinary offset. </param>
	private double GetVerticalOverlap(NonoverlappingRectangleCollectiοn polygon, Point offset = default(Point))
	{
		Contract.Requires(polygon != null);

		double largestOverlap = 0;
		foreach (Rect r in polygon)
			foreach (Rect element in this)
			{
				double overlap = element.Bottom - (r.Y + offset.Y);
				if (overlap > largestOverlap)
					largestOverlap = overlap;
			}
		return largestOverlap;
	}
	/// <summary> Expands this rectangle collection by specified amounts in the horizontal and vertical directions. </summary>
	/// <param name="xExpansion"> The horizontal expansion. </param>
	/// <param name="yExpansion"> The vertical expansion. </param>
	public void Expand(double xExpansion, double yExpansion)
	{
		Contract.Requires(xExpansion >= 0);
		Contract.Requires(yExpansion >= 0);

		Rect[] original = new Rect[this.Count];
		this.rectangles.CopyTo(original);

		foreach (Rect r in original)
		{
			Rect expandedR = new Rect(x: r.Left - xExpansion,
									  y: r.Top - yExpansion,
									  width: r.Width + 2 * xExpansion,
									  height: r.Height + 2 * yExpansion);
			this.Add(expandedR);
		}
	}
	/// <summary> Removes all area in this rectangle collection that lies outside of the specified bounding rectangle. </summary>
	/// <param name="bound"> The rectangle to bound this area. </param>
	public void Truncate(Rect bound)
	{
		this.Remove(new Rect(new Point(double.NegativeInfinity, double.NegativeInfinity), new Point(double.PositiveInfinity, bound.Top)));
		this.Remove(new Rect(new Point(double.NegativeInfinity, double.PositiveInfinity), new Point(double.PositiveInfinity, bound.Bottom)));
		this.Remove(new Rect(new Point(double.NegativeInfinity, bound.Bottom), bound.Location));
		this.Remove(new Rect(bound.TopRight, new Point(double.PositiveInfinity, bound.Bottom)));
	}
	/// <summary> Removes the area in the specified rectangle from the current polygon. </summary>
	/// <param name="rect"></param>
	public void Remove(Rect rect)
	{
		this.rectangles = this.rectangles.Select(r => RectangleExtensions.Subtract(r, rect)).Concat().ToList();

		this.Simplify();//this simplications are probably redundant, but are added for the sake of robustness
	}
	/// <summary> Gets all rectangles in this polygon. </summary>
	public IEnumerator<Rect> GetEnumerator()
	{
		return this.rectangles.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Offset(Point p)
	{
		Contract.Requires(!double.IsNaN(p.X));
		Contract.Requires(!double.IsNaN(p.Y));
		for (int i = 0; i < this.Count; i++)
		{
			var copy = this.rectangles[i];
			copy.Offset(p.X, p.Y);
			this.rectangles[i] = copy;
		}
	}
	/// <summary> Multiplies all coordinates in this polygon by the specified factor. </summary>
	/// <param name="factor"> The factor to scale this polygon with. Must be a non-zero number. </param>
	public void Scale(double factor)
	{
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		Contract.Requires(factor != 0);
		Contract.Requires(!double.IsInfinity(factor));
		Contract.Requires(!double.IsNaN(factor));

		for (int i = 0; i < this.rectangles.Count; i++)
		{
			var copy = this.rectangles[i];
			copy.Scale(factor, factor);
			this.rectangles[i] = copy;
		}
	}
	/// <summary> Centers this polygon around the specified center, i.e. such that this.Right = center + this.Width / 2; </summary>
	public void CenterHorizontally(double center)
	{
		Contract.Requires(!double.IsInfinity(center));
		Contract.Requires(!double.IsNaN(center));

		double currentCenter = (this.Right - this.Left) / 2;
		double offset = center - currentCenter;
		this.Offset(new Point(offset, 0));
	}

	public override string ToString()
	{
		return string.Format("TopLeft: {0}, BottomRight: {1}", TopLeft, BottomRight);
	}

	/// <summary> Gets the smallest rectangle encapsulating all rectangles in this rectangle collection. </summary>
	public Rect Bounds
	{
		get
		{
			if (this.rectangles.Count == 0)
				return Rect.Empty;
			else if (this.rectangles.Count == 1)
				return this.rectangles[0];
			else
			{
				double left = double.MaxValue;
				double top = double.MaxValue;
				double right = double.MinValue;
				double bottom = double.MinValue;

				foreach (Rect r in this.rectangles)
				{
					left = Math.Min(left, r.Left);
					top = Math.Min(top, r.Top);
					right = Math.Max(right, r.Right);
					bottom = Math.Max(bottom, r.Bottom);
				}

				return new Rect(new Point(left, top), new Point(right, bottom));
			}
		}
	}
	/// <summary> Gets the smallest rectangle encapsulating all specified polygons. </summary>
	/// <param name="polygons"> The polygons to encapsulate. </param>
	public static Rect GetBounds(IEnumerable<NonoverlappingRectangleCollectiοn> polygons)
	{
		Contract.Requires(polygons != null);
		//Contract.Requires(Contract.ForAll(polygons, polygon => polygon != null));//disabled due to annoying resharper warning

		return new NonoverlappingRectangleCollectiοn(polygons.Select(polygon => polygon.Bounds)).Bounds;
	}

	public void CheckInvariants()
	{
		foreach (Rect r in this.rectangles)
		{
			Contract.Invariant(!double.IsNaN(r.Left));
			Contract.Invariant(!double.IsNaN(r.Right));
			Contract.Invariant(!double.IsNaN(r.Top));
			Contract.Invariant(!double.IsNaN(r.Bottom));
			Contract.Invariant(!double.IsNaN(r.Width));
			Contract.Invariant(!double.IsNaN(r.Height));
		}

		//sanity checks... not really invariants:
		Contract.Assert(Math.Abs(Top) < 10000);
		Contract.Assert(Math.Abs(Bottom) < 10000);
		Contract.Assert(Math.Abs(Left) < 10000);
		Contract.Assert(Math.Abs(Right) < 10000);

	}
	public NonoverlappingRectangleCollectiοn WithOffset(Point offset)
	{
		var result = new NonoverlappingRectangleCollectiοn(this);
		result.Offset(offset);
		return result;
	}
}
