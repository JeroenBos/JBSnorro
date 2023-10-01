using JBSnorro.Geometry.Integer;

namespace JBSnorro.Algorithms;

public static class Floodfill
{
    /// <summary> </summary>
    /// <param name="area"> A bunch of ones and zeroes denoting regions and non-regions, respectively. </param>
    public static List<Rectangle> DivideMapInAreas(int[,] area)
    {
        int height = area.GetLength(0);
        int width = area.GetLength(1);
        List<Rectangle> areaBoundingsRects = new();

        int curArea = 1;

        bool fill = false;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int Area = area[y, x];
                if (Area == 0)
                {
                    fill = false;
                }
                else
                {
                    if (Area == 1)
                    {
                        if (fill)
                        {
                            Area = curArea;
                            area[y, x] = curArea;
                        }
                        else
                        {
                            curArea++;

                            var bounds = SetBorder(new Point(x, y), curArea, area);
                            areaBoundingsRects.Add(bounds);
                            fill = true;
                            continue;
                        }
                    }
                    else
                    {
                        curArea = Area;
                        fill = true;
                    }
                    // if (x != width - 1 && area[y, x] == 0)
                    // {
                    // var bounds = SetBorder(new Point(x, y), curArea, area);
                    // areaBoundingsRects.Add(bounds);
                    // }
                }
            }
            fill = false;
        }
        return areaBoundingsRects;
    }
    public static List<Rectangle> DivideMapInAreas(Func<Point, bool> isArea, int width, int height)
    {
        List<Rectangle> areaBoundingsRects = new();
        HashSet<Point> alreadyBeen = new();
        int curArea = 1;

        bool fill = false;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var p = new Point(x, y);
                if (!isArea(p))
                {
                    fill = false;
                }
                else
                {
                    if (!alreadyBeen.Contains(p))
                    {
                        if (fill)
                        {
                        }
                        else
                        {
                            curArea++;

                            var bounds = SetBorder(new Point(x, y), isArea, alreadyBeen, width, height);
                            areaBoundingsRects.Add(bounds);
                            fill = true;
                            continue;
                        }
                    }
                    else
                    {
                        fill = true;
                    }
                }
            }
            fill = false;
        }
        return areaBoundingsRects;
    }
    private static Rectangle SetBorder(Point startPosition, int curArea, int[,] area)
    {
        var boundingRect = new Rectangle(startPosition, 1, 1);
        Point curPosi = startPosition;
        int D = 2;
        do
        {
            area[curPosi.Y, curPosi.X] = curArea;
            boundingRect = boundingRect.Add(curPosi);
            for (int d = 0; d < 8; d++)
            {
                Point p = curPosi + getOffset(D + d);
                if (!p.WithinBounds(area.GetLength(1), area.GetLength(0)))
                    continue;
                if (area[p.Y, p.X] != 0)
                {
                    curPosi = p;
                    D = D + d + 6 & 7;
                    break;
                }
            }
        } while (curPosi != startPosition);
        return boundingRect;
    }
    private static Rectangle SetBorder(Point startPosition, Func<Point, bool> isArea, HashSet<Point> alreadyBeen, int width, int height)
    {
        var boundingRect = new Rectangle(startPosition, 1, 1);
        Point p = startPosition;
        int D = 2;
        do
        {
            alreadyBeen.Add(p);
            boundingRect = boundingRect.Add(p);
            for (int d = 0; d < 8; d++)
            {
                Point next = p + getOffset(D + d);
                if (!next.WithinBounds(width, height))
                    continue;
                if (isArea(next))
                {
                    p = next;
                    D = D + d + 6 & 7;
                    break;
                }
            }
        } while (p != startPosition);
        return boundingRect;
    }
    private static Point getOffset(int i)
    {
        // I envision the plane with (0, 0) being in the top right corner
        return (i % 8) switch
        {
            0 => new Point(0, -1),
            1 => new Point(1, -1),
            2 => new Point(1, 0),
            3 => new Point(1, 1),
            4 => new Point(0, 1),
            5 => new Point(-1, 1),
            6 => new Point(-1, 0),
            7 => new Point(-1, -1),
            _ => throw new ArgumentOutOfRangeException(nameof(i))
        };
    }
}
