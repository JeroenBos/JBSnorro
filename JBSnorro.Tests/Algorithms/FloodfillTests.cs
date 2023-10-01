﻿using JBSnorro.Algorithms;
using JBSnorro.Diagnostics;
using JBSnorro.Geometry.Integer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Algorithms;

[TestClass]
public class FloodFillTests
{
    [TestMethod]
    public void TestFloodfillA()
    {
        var rects = Floodfill.DivideMapInAreas(new int[,] { { 0 } });

        Contract.AssertSequenceEqual(rects, Array.Empty<Rectangle>());
    }
    [TestMethod]
    public void TestFloodfillB()
    {
        var rects = Floodfill.DivideMapInAreas(new int[,] { { 1 } });

        if (!rects.SequenceEqual(new[] { new Rectangle(0, 0, 1, 1) }))
            throw new Exception();
    }
    [TestMethod]
    public void TestFloodfillC()
    {
        var rects = Floodfill.DivideMapInAreas(
            new int[,] {
                { 0, 0, 0, 0, 0, },
                { 1, 1, 0, 0, 0, },
                { 0, 1, 1, 0, 0, },
                { 0, 0, 0, 0, 0, }
            });

        if (!rects.SequenceEqual(new[] { new Rectangle(0, 1, 3, 2) }))
            throw new Exception();
    }

    [TestMethod]
    public void TestFloodfillD()
    {
        var rects = Floodfill.DivideMapInAreas(
            new int[,] {
                { 0, 0, 1, 0, 0, },
                { 1, 1, 1, 1, 1, },
                { 0, 1, 1, 0, 0, },
                { 0, 1, 0, 0, 1, }
            });

        if (!rects.SequenceEqual(new[] { new Rectangle(0, 0, 5, 4), new Rectangle(4, 3, 1, 1) }))
            throw new Exception();
    }

    [TestMethod]
    public void TestFloodfillE()
    {
        var map = new int[,] {
                { 0, 0, 0, 0, 0, },
                { 1, 1, 0, 0, 0, },
                { 0, 1, 1, 0, 0, },
                { 0, 0, 0, 0, 0, }
            };
        var rects = Floodfill.DivideMapInAreas(p => map[p.Y, p.X] != 0, map.GetLength(1), map.GetLength(0));


        if (!rects.SequenceEqual(new[] { new Rectangle(0, 1, 3, 2) }))
            throw new Exception();
    }

    [TestMethod]
    public void TestFloodfillF()
    {
        var map = new int[,] {
                { 0, 0, 1, 0, 0, },
                { 1, 1, 1, 1, 1, },
                { 0, 1, 1, 0, 0, },
                { 0, 1, 0, 0, 1, }
            };
        var rects = Floodfill.DivideMapInAreas(p => map[p.Y, p.X] != 0, map.GetLength(1), map.GetLength(0));

        if (!rects.SequenceEqual(new[] { new Rectangle(0, 0, 5, 4), new Rectangle(4, 3, 1, 1) }))
            throw new Exception();
    }
}
