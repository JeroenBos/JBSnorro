using System.Diagnostics;

namespace JBSnorro.Geometry.Integer;

public record struct Point
{
    public int X { get; }
    public int Y { get; }
    [DebuggerHidden]
    public Point(int x, int y) => (X, Y) = (x, y);
    [DebuggerHidden]
    public bool WithinBounds(int width, int height)
    {
        return 0 <= X && X < width
            && 0 <= Y && Y < height;
    }
    [DebuggerHidden] public static Point operator +(Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
}

