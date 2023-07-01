using static JBSnorro.Diagnostics.Contract;
using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBNA.Tests;

[TestClass]
public class BitReaderTests
{
    [TestMethod]
    public void ReadDoubleFrom3TrueBits()
    {
        IBitReader bitReader = new BitReader(new BitArray(new bool[] { true, true, true }));

        var value = bitReader.ReadDouble(3);

        Contract.Assert(value == -8);
    }
    [TestMethod]
    public void ReadDoubleFrom3FalseBits()
    {
        IBitReader bitReader = new BitReader(new BitArray(new bool[] { false, false, false }));

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
                IBitReader bitreader = new BitReader(bitarray);
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
    [TestMethod]
    public void Can_Construct()
    {
        var reader = new BitReader(new BitArray());
        Assert(reader.Length == 0);
    }
    [TestMethod]
    public void Can_Read_FalseBit()
    {
        IBitReader reader = new BitReader(new BitArray(new bool[] { false }));
        Assert(reader.ReadBit() == false);
    }
    [TestMethod]
    public void Can_Read_TrueBit()
    {
        IBitReader reader = new BitReader(new BitArray(new bool[] { true }));
        Assert(reader.ReadBit() == true);
    }
    [TestMethod]
    public void Can_Read_Zero_Byte()
    {
        IBitReader reader = new BitReader(new[] { 0b0UL }, 8);
        Assert(reader.ReadByte() == 0);
    }

    [TestMethod]
    public void Can_Read_One_Byte()
    {
        IBitReader reader = new BitReader(new[] { 0b0000_0001UL }, 8);
        Assert(reader.ReadByte() == 1);
    }
    [TestMethod]
    public void Can_Read_Two_Byte()
    {
        IBitReader reader = new BitReader(new[] { 0b0000_0010UL }, 8);
        Assert(reader.ReadByte() == 2);
    }

    [TestMethod]
    public void Can_Read_Two_ULong()
    {
        IBitReader reader = new BitReader(new[] { 0b0000_0010UL }, 64);
        Assert(reader.ReadUInt64() == 2);
    }
    [TestMethod]
    public void Reading_bits_is_successive()
    {
        IBitReader reader = new BitReader(new[] { 0b0000_0110UL }, 8);
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
        IBitReader reader = new BitReader(new[] { 0b1111_0000_0000_0110UL }, 16);
        Assert(reader.ReadByte() == 0b110);
        Assert(reader.RemainingLength == 8);
        Assert(reader.ReadByte() == 0b1111_0000);
        Assert(reader.RemainingLength == 0);
    }
    [TestMethod]
    public void Reading_bits_and_bytes_is_successive()
    {
        IBitReader reader = new BitReader(new[] { 0b1101_0111_0100_0000_0110UL }, 20);
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
        IBitReader reader = new BitReader(new[] { (0b1001UL << 60) | 1234, 0b1100UL }, 100);
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
        var baseReader = new BitReader(new BitArray(new ulong[] { 0b1111100000 }, 10));
        var reader = new FloatingPointBitReader(baseReader, 0, 1UL << 10);

        var result = reader.ReadDouble(10);

        Contract.Assert(result == 0b1111100000);
    }
}