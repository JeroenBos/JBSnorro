using System.Diagnostics;

namespace JBSnorro.Geometry.Integer;

/// <summary> A rectangle that is excluding its right and bottom coordinates. </summary>
public record struct Rectangle
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    [DebuggerHidden] public Point TopLeft => new Point(X, Y);
    [DebuggerHidden]
    public Rectangle(int x, int y, int width, int height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    public Rectangle(Point p, int width, int height) : this(p.X, p.Y, width, height) { }
    public int Right => X + Width;
    public int Bottom => Y + Height;

    [DebuggerHidden]
    public static Rectangle From(int left, int right, int top, int bottom)
    {
        return new Rectangle(left, top, right - left, bottom - top);
    }
    /// <summary> Returns a new rectangle that extends to the specified point. </summary>
    [DebuggerHidden]
    public Rectangle Add(Point p)
    {
        return From(
            Math.Min(p.X, X),
            Math.Max(p.X + 1, Right),
            Math.Min(p.Y, Y),
            Math.Max(p.Y + 1, Bottom)
        );
    }

}