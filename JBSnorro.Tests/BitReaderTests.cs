using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class BitReaderTests
{
    [TestMethod]
    public void ReadDoubleFrom3TrueBits()
    {
        var bitReader = new BitReader(new BitArray(new bool[] { true, true, true }));

        var value = bitReader.ReadDouble(3);

        Contract.Assert(value == 4);
    }
    [TestMethod]
    public void ReadDoubleFrom3FalseBits()
    {
        var bitReader = new BitReader(new BitArray(new bool[] { false, false, false }));

        var value = bitReader.ReadDouble(3);

        Contract.Assert(value == -2);
    }

    [TestMethod]
    public void SensibilityCheckOnSpecificity()
    {
        const double init = 5;
        const double min = 0.5;
        var ranges = new (int length, double maxAbsoluteRange, double minAbsolutePrecision)[] {
            (3, init * double.Pow(2, 1), min * double.Pow(2, -1)),
            (4, init * double.Pow(2, 2), min * double.Pow(2, -2)),
            (5, init * double.Pow(2, 3), min * double.Pow(2, -3)),
            (6, init * double.Pow(2, 4), min * double.Pow(2, -4)),
        };

        foreach (var (bitLength, maxRange, minPrecision) in ranges)
        {
            Combinatorics
            bool[] bools= new bool[bitLength];
            
        }
    }
}
