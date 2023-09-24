using static JBSnorro.Diagnostics.Contract;
using JBSnorro;
using JBSnorro.Collections.Bits;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using JBSnorro.Collections.Bits.Internals;

namespace Tests.JBSnorro.Collections.Bits;

[TestClass]
public abstract class IFloatingPointBitReaderTests
{
    public abstract IFloatingPointBitReader CreateFloatingPointBitReader(BitArray bitArray);
    public virtual IFloatingPointBitReader CreateFloatingPointBitReader(bool[] bits) => CreateFloatingPointBitReader(new BitArray(bits));
    public abstract IFloatingPointBitReaderEncoding Encoding { get; }


    [TestMethod]
    public void ReadDoubleFrom3FalseBits()
    {
        var bitReader = CreateFloatingPointBitReader(new[] { false, false, false });

        var value = bitReader.ReadDouble(3);

        Assert(value == 0);
    }
    /// <summary>
    /// I.e. that distinct bit ranges are mapped to distinct values.
    /// </summary>
    [TestMethod]
    public virtual void Is_injective()
    {
        var set = new HashSet<double>();
        for (ulong u = 0; u < 100; u++)
        {
            int length = Math.Max(3, u.CountBits());
            var reader = CreateFloatingPointBitReader(new BitArray(new ulong[] { u }, length));
            var result = reader.ReadDouble(length);

            Assert(!set.Contains(result));

            set.Add(result);
        }
    }
    [TestMethod]
    public virtual void Numbers_remain_invariant_prepended_with_zeroes()
    {
        var samples = new ulong[] { 0b111111, 0b111, 0b101, 0b1101 };
        foreach (var sample in samples)
        {
            int sampleLength = sample.CountBits();
            var referenceReader = CreateFloatingPointBitReader(new BitArray(new ulong[] { sample }, sampleLength));
            var expected = referenceReader.ReadDouble(sampleLength);
            for (int i = 0; i < 5; i++)
            {
                sampleLength++;
                var reader = CreateFloatingPointBitReader(new BitArray(new ulong[] { sample }, sampleLength));
                var actual = reader.ReadDouble(sampleLength);
                Assert(actual == expected);
            }
        }
    }

    [TestMethod]
    public void GetMinimum_gets_the_minimum()
    {
        var list = new List<double>();
        for (int bitCount = IFloatingPointBitReader.MIN_BIT_COUNT; bitCount < 11; bitCount++)
        {
            var actual = Encoding.GetMinValue(bitCount);
            var expected = getAllPossibleNumbers(bitCount).Min();
            list.Add(expected);

            Contract.Assert(actual == expected);
        }
    }
    [TestMethod]
    public void GetMaximum_gets_the_maximum()
    {
        for (int bitCount = IFloatingPointBitReader.MIN_BIT_COUNT; bitCount < 11; bitCount++)
        {
            var actual = Encoding.GetMaxValue(bitCount);
            var expected = getAllPossibleNumbers(bitCount).Max();

            Contract.Assert(actual == expected);
        }
    }

    private IEnumerable<double> getAllPossibleNumbers(int bitCount)
    {
        for (ulong bits = 0; bits < (1UL << bitCount); bits++)
        {
            var reader = CreateFloatingPointBitReader(new BitArray(new ulong[] { bits }, bitCount));
            var number = reader.ReadDouble(bitCount);
            yield return number;
        }
    }

    /// <summary>
    /// There must be at least one element in every range of length δ between [start, end]:
    /// </summary>
    [TestMethod]
    public virtual void Is_sufficiently_dense()
    {
        const int bitCount = 9;
        const double δ = 0.1;
        const double rangeStart = -10;
        const double rangeEnd = 10;


        var list = getAllPossibleNumbers(bitCount).Order().ToArray();

        static double? findInRange(IEnumerable<double> sortedSequence, double start, double end)
        {
            foreach (var item in sortedSequence)
            {
                if (item > end)
                {
                    break;
                }
                if (start < item && item < end)
                {
                    return item;
                }
            }
            return null;
        }

        double? temp;
        for (double cursor = rangeStart; cursor < rangeEnd; cursor = temp.Value)
        {
            temp = findInRange(list, start: cursor, end: cursor + δ);

            Contract.Assert(temp != null, $"Not sufficiently dense at {cursor}");
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
        IBitReader reader = Create(new[] { 0b1001UL << 60 | 1234, 0b1100UL }, 100);
        var x = reader.ReadUInt64(bitCount: 60);
        Assert(x == 1234);
        var y = reader.ReadByte();
        Assert(y == 0b1100_1001);
    }
    [TestMethod]
    public void CaseTest()
    {
        var data = new BitArray(new ulong[] { 0b1111100000 }, 10);
        var reader = data[..].ToBitReader();

        var result = reader.ReadUInt64(10);

        Assert(result == 0b1111100000);
    }

}

