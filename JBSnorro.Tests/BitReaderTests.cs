﻿using static JBSnorro.Diagnostics.Contract;
using JBSnorro;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using JBSnorro.Collections.Bits;

namespace JBNA.Tests;

[TestClass]
public class IFloatingPointBitReaderTests
{
    private static IFloatingPointBitReader Create(BitArray array)
    {
        // to prove that this only tests IBitReader functions, but we need to create an instance, we encapsulate the call to SomeBitReader:
        return IFloatingPointBitReader.Create(array.ToBitReader());
    }

    [TestMethod]
    public void ReadDoubleFrom3TrueBits()
    {
        IFloatingPointBitReader bitReader = Create(new BitArray(new bool[] { true, true, true }));

        var value = bitReader.ReadDouble(3);

        Contract.Assert(value == -8);
    }
    [TestMethod]
    public void ReadDoubleFrom3FalseBits()
    {
        IFloatingPointBitReader bitReader = Create(new BitArray(new bool[] { false, false, false }));

        var value = bitReader.ReadDouble(3);

        Contract.Assert(value == 0);
    }

    [TestMethod]
    public void SensibilityCheckOnSpecificity()
    {
        const double init = 8;
        const double min = 0.5;
        // var ranges = Enumerable.Range(3, 8).Select(i => (i, double.Pow(2, i + 4), double.Pow(2, -i)));
        var ranges = new (int length, double maxAbsoluteRange, double minAbsolutePrecision)[] {
            (3, init * double.Pow(2, 1), min * double.Pow(2, -1)),
            (4, init * double.Pow(2, 2), min * double.Pow(2, -2)),
            (5, init * double.Pow(2, 2), min * double.Pow(2, -2)),
            (6, init * double.Pow(2, 4), min * double.Pow(2, -4)),
            (7, init * double.Pow(2, 7), min * double.Pow(2, -7)),
            (8, init * double.Pow(2, 8), min * double.Pow(2, -7)),
            (9, init * double.Pow(2, 16), min * double.Pow(2, -14)),
        };

        foreach (var (bitLength, maxRange, minPrecision) in ranges)
        {
            int combinationsCount = (int)double.Pow(2, bitLength);

            var allBitCombinations = Enumerable.Range(0, combinationsCount).Select(i => new BitArray(new[] { (ulong)i }, bitLength)).ToList();

            foreach (var bitarray in allBitCombinations)
            {
                IFloatingPointBitReader bitreader = Create(bitarray);
                var value = bitreader.ReadDouble(bitLength);
                var absValue = double.Abs(value);

                if (absValue == 0)
                    Console.Write("0");
                else
                    Console.Write(string.Format("{0:#,0.000}", value).TrimEnd('0', '.'));
                Console.Write(", ");

                Contract.Assert(absValue <= maxRange);
                if (absValue != 0)
                {
                    Contract.Assert(absValue >= minPrecision);
                }
            }
            Console.WriteLine();
        }
    }
}


[TestClass]
public class BinaryReaderTests
{
    [DebuggerHidden]
    private static IBitReader Create(ulong[] data, ulong length, ulong startBitIndex = 0)
    {
        return Create(BitArray.FromRef(data, length), startBitIndex);
    }
    [DebuggerHidden]
    private static IBitReader Create(BitArray data, ulong startBitIndex = 0)
    {
        // to prove that this only tests IBitReader functions, but we need to create an instance, we encapsulate the call to SomeBitReader:
        return IBitReader.Create(data, startBitIndex);
    }

    [TestMethod]
    public void Can_Construct()
    {
        var reader = Create(new BitArray());
        Assert(reader.Length == 0);
    }
    [TestMethod]
    public void Can_Read_FalseBit()
    {
        IBitReader reader = Create(new BitArray(new bool[] { false }));
        Assert(reader.ReadBit() == false);
    }
    [TestMethod]
    public void Can_Read_TrueBit()
    {
        IBitReader reader = Create(new BitArray(new bool[] { true }));
        Assert(reader.ReadBit() == true);
    }
    [TestMethod]
    public void Can_Read_Zero_Byte()
    {
        IBitReader reader = Create(new[] { 0b0UL }, 8);
        Assert(reader.ReadByte() == 0);
    }

    [TestMethod]
    public void Can_Read_One_Byte()
    {
        IBitReader reader = Create(new[] { 0b0000_0001UL }, 8);
        var actual = reader.ReadByte();
        Assert(actual == 1);
    }
    [TestMethod]
    public void Can_Read_Two_Byte()
    {
        IBitReader reader = Create(new[] { 0b0000_0010UL }, 8);
        Assert(reader.ReadByte() == 2);
    }

    [TestMethod]
    public void Can_Read_Two_ULong()
    {
        IBitReader reader = Create(new[] { 0b0000_0010UL }, 64);
        Assert(reader.ReadUInt64() == 2);
    }
    [TestMethod]
    public void Reading_bits_is_successive()
    {
        IBitReader reader = Create(new[] { 0b0000_0110UL }, 8);
        Assert(reader.ReadBit() == false);
        Assert(reader.ReadBit() == true);
        Assert(reader.ReadBit() == true);
        Assert(reader.ReadBit() == false);
        Assert(reader.ReadBit() == false);
        Assert(reader.RemainingLength == 3);
    }
    [TestMethod]
    public void Reading_bytes_is_successive()
    {
        IBitReader reader = Create(new[] { 0b1111_0000_0000_0110UL }, 16);
        Assert(reader.ReadByte() == 0b110);
        Assert(reader.RemainingLength == 8);
        Assert(reader.ReadByte() == 0b1111_0000);
        Assert(reader.RemainingLength == 0);
    }
    [TestMethod]
    public void Reading_bits_and_bytes_is_successive()
    {
        IBitReader reader = Create(new[] { 0b1101_0111_0100_0000_0110UL }, 20);
        Assert(reader.ReadBit() == false);
        Assert(reader.RemainingLength == 19);
        Assert(reader.ReadByte() == 0b0000_0011);
        Assert(reader.RemainingLength == 11);
        Assert(reader.ReadBit() == false);
        Assert(reader.ReadBit() == true);
        Assert(reader.ReadBit() == false);
        Assert(reader.RemainingLength == 8);
        Assert(reader.ReadByte() == 0b1101_0111);
    }
    [TestMethod]
    public void Can_read_bytes_over_ulong_crossing()
    {
        IBitReader reader = Create(new[] { (0b1001UL << 60) | 1234, 0b1100UL }, 100);
        var x = reader.ReadUInt64(bitCount: 60);
        Assert(x == 1234);
        var y = reader.ReadByte();
        Assert(y == 0b1100_1001);
    }
}



[TestClass]
public class FloatingPointBitReaderTests
{
    [TestMethod]
    public void SimpleTest()
    {
        var data = new BitArray(new ulong[] { 0b1111100000 }, 10);
        var reader = data[..].ToBitReader();

        var result = reader.ReadUInt64(10);

        Contract.Assert(result == 0b1111100000);
    }
}