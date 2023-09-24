using System.Diagnostics;

namespace JBSnorro.Collections.Bits.Internals;

internal class ULongLikeFloatingPointBitReader : IFloatingPointBitReader
{
    internal static double ReadDouble(IBitReader reader, int bitCount)
    {
        if (bitCount < IFloatingPointBitReader.MIN_BIT_COUNT || bitCount > 64)
            throw new ArgumentOutOfRangeException(nameof(bitCount));

        ulong value = reader.ReadUInt64(bitCount);
        if (value == 0)
            return 0;
        // under the current scheme, 1 and 2 map to zero, which already exists. I just choose these numbers randomly:
        if (value == 1)
            return -0.9;
        if (value == 2)
            return 0.9;
        value--;
        bool sign = (value & 1) == 0;
        value >>= 1;
        double result = 0;
        for (int i = 0; i <= Math.Min(31, bitCount / 2) + 1; i++)
        {
            bool hasSignificantBit = (value & 1UL << 2 * i) != 0;
            bool hasInsignificantBit = (value & 1UL << 2 * i + 1) != 0;
            result += Math.Pow(2, i) * (hasSignificantBit ? 1 : 0);
            result += Math.Pow(2, -i - 1) * (hasInsignificantBit ? 1 : 0);
        }
        return result * (sign ? 1 : -1);
    }
    internal static double ComputeMax(int bitCount)
    {
        return computeExtrema(bitCount).Max();
    }
    internal static double ComputeMin(int bitCount)
    {
        return computeExtrema(bitCount).Min();
    }
    private static IEnumerable<double> computeExtrema(int bitCount)
    {
        // can't be bothered to do this theoretically. Just pick the correct extremum of the extrema
        var sequences = new[]
{
            Enumerable.Range(0, bitCount).Select(i => i == bitCount - 1),
            Enumerable.Range(0, bitCount).Select(_ => true),
            Enumerable.Range(0, bitCount).Select(i => i == bitCount - 1 || i == bitCount - 2),
        };
        return sequences.Select(sequence => computeHelper(sequence, bitCount));

        static double computeHelper(IEnumerable<bool> sequence, int bitCount)
        {
            var reader = new BitArray(sequence).ToBitReader(IFloatingPointBitReaderEncoding.ULongLike);
            return ReadDouble(reader, bitCount);
        }
    }

    public IBitReader Reader { get; }

    public ULongLikeFloatingPointBitReader(IBitReader reader)
    {
        Reader = reader;
    }

    public double ReadDouble(int bitCount)
    {
        return ULongLikeFloatingPointBitReader.ReadDouble(this.Reader, bitCount);
    }
    IBitReader IBitReader.Clone()
    {
        return new ULongLikeFloatingPointBitReader(Reader.Clone());
    }
    static IFloatingPointBitReaderEncoding IFloatingPointBitReader.Encoding => IFloatingPointBitReaderEncoding.ULongLike;
}

