#nullable enable
using JBSnorro.Collections.Bits.Internals;
using JBSnorro.Diagnostics;
using System.Diagnostics;

namespace JBSnorro.Collections.Bits;

public interface IFloatingPointBitReader : IBitReader
{
    public static IFloatingPointBitReader Create(IBitReader reader, Func<int /*bitCount*/, IBitReader, double>? readDouble = null)
    {
        return new FloatingPointBitReader(reader, readDouble ?? DefaultReadDouble);
    }
    private static double DefaultReadDouble(int bitCount, IBitReader self)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (self.RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("double");

        // the idea is that if the mantissa is filled, then the bits are just becoming less and less relevant
        // but before some bitCount, there simply isn't enough bits to have this strategy. Something more complicated (or simpler) is needed
        ulong originalRemainingLength = self.RemainingLength;
        int significantBitCount = Math.Max(2, bitCount / 2);
        int exponentBitCount = bitCount - significantBitCount;

        //IBitReader self = this;
        var significant = self.ReadInt64(significantBitCount);
        Contract.Assert(self.RemainingLength + (ulong)significantBitCount == originalRemainingLength);
        var exponent = exponentBitCount == 0 ? 1 : exponentBitCount == 1 ? (double)self.ReadUInt64(exponentBitCount) : self.ReadInt64(exponentBitCount);

        var value = 2 * significant * double.Pow(2, exponent);
        return value;
    }

    [DebuggerHidden]
    public Half ReadHalf(int bitCount = 16)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("Half");

        // half has 5 bits exponent
        return (Half)ReadDouble(bitCount);
    }
    [DebuggerHidden]
    public float ReadSingle(int bitCount = 32)
    {
        if (bitCount < 2 || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("float");

        // float has 8 bits exponent
        return (float)ReadDouble(bitCount);
    }
    public double ReadDouble(int bitCount = 64);
}