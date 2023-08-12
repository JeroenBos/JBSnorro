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
        if (value == 1)
            return -1;
        if (value == 2)
            return 1;
        value--;
        bool sign = (value & 1) == 0;
        value >>= 1; 
        double result = 0;
        for (int i = 0; i <= Math.Min(31, bitCount / 2) + 1; i++)
        {
            bool hasSignificantBit = (value & 1UL << 2 * i) != 0;
            bool hasInsignificantBit = (value & 1UL << 2 * i + 1) != 0;
            result += Math.Pow(2, i + 1) * (hasSignificantBit ? 1 : 0);
            result += Math.Pow(2, -i - 1) * (hasInsignificantBit ? 1 : 0);
        }
        return result * (sign ? 1 : -1);
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
}

