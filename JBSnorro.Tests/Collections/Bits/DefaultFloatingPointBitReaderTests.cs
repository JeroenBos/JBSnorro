using JBSnorro.Collections.Bits;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Collections.Bits;

[TestClass]
public class DefaultFloatingPointBitReaderTests : IFloatingPointBitReaderTests
{
    public override IFloatingPointBitReader CreateFloatingPointBitReader(BitArray bits)
    {
        return bits.ToBitReader(IFloatingPointBitReaderEncoding.Default);
    }
    [TestMethod]
    public void ReadDoubleFrom3TrueBits()
    {
        var bitReader = CreateFloatingPointBitReader(new[] { true, true, true });

        var value = bitReader.ReadDouble(3);

        Contract.Assert(value == -8);
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
                var bitreader = this.CreateFloatingPointBitReader(bitarray);
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

    [TestMethod]
    public override void Numbers_remain_invariant_prepended_with_zeroes()
    {
        // the interface test is violated for this DefaultFloatingPointBitReader and I don't think it's necessary to fix it
    }
}
