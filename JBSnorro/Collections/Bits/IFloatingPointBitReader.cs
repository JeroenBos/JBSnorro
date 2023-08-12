﻿using JBSnorro.Collections.Bits.Internals;
using JBSnorro.Diagnostics;
using System.Diagnostics;

namespace JBSnorro.Collections.Bits;

public enum IFloatingPointBitReaderEncoding
{
    /// <summary>
    /// Reads an exponent and then mantissa.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Reads a ulong and divides bits into significant (i.e. big numbers) and insignificant (high precision) ones.
    /// </summary>
    ULongLike = 1,
}
public interface IFloatingPointBitReader : IBitReader
{
    public const int MIN_BIT_COUNT = 3;
    /// <summary>
    /// Creates a <see cref="IFloatingPointBitReader"/> from a custom floating-point reading function.
    /// </summary>
    /// <param name="reader">The bits.</param>
    /// <param name="customReadDouble"> The function taking a capable of converting a specified number (=first argument) of bits from a <see cref="BitReader"/> to a floating-point number.</param>
    public static IFloatingPointBitReader Create(IBitReader reader, ReadDoubleDelegate? customReadDouble = null)
    {
        return new FloatingPointBitReader(reader, customReadDouble ?? DefaultReadDouble);
    }
    /// <summary>
    /// Creates a <see cref="IFloatingPointBitReader"/> from a predefined list of floating-point reading functions.
    /// </summary>
    /// <param name="reader">The bit reader to read bits from.</param>
    public static IFloatingPointBitReader Create(IBitReader reader, IFloatingPointBitReaderEncoding readerType)
    {
        Contract.Requires(Enum.IsDefined(readerType));

        return readerType switch
        {
            IFloatingPointBitReaderEncoding.Default => new FloatingPointBitReader(reader, DefaultReadDouble),
            IFloatingPointBitReaderEncoding.ULongLike => new ULongLikeFloatingPointBitReader(reader),
            _ => throw new UnreachableException(),
        };
    }
    internal static double DefaultReadDouble(IBitReader self, int bitCount)
    {
        if (bitCount < MIN_BIT_COUNT || bitCount > 64)
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
        if (bitCount < MIN_BIT_COUNT || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("Half");

        // half has 5 bits exponent
        return (Half)ReadDouble(bitCount);
    }
    [DebuggerHidden]
    public float ReadSingle(int bitCount = 32)
    {
        if (bitCount < MIN_BIT_COUNT || bitCount > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (RemainingLength < (ulong)bitCount)
            throw new InsufficientBitsException("float");

        // float has 8 bits exponent
        return (float)ReadDouble(bitCount);
    }
    public double ReadDouble(int bitCount = 64);

    #region IBitReader Members
    protected IBitReader Reader { get; }
    IBitReader IBitReader.this[Range range]
    {
        get => Reader[range];
    }
    ulong IBitReader.Length
    {
        get => Reader.Length;
    }
    ulong IBitReader.Position
    {
        get => Reader.Position;
    }
    ulong IBitReader.ReadUInt64(int bitCount)
    {
        return Reader.ReadUInt64(bitCount);
    }
    void IBitReader.Seek(ulong bitIndex)
    {
        Reader.Seek(bitIndex);
    }
    #endregion
}


public delegate double ReadDoubleDelegate(IBitReader bitReader, int bitCount);