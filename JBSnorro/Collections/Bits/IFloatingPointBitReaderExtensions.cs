using JBSnorro.Collections.Bits.Internals;
using JBSnorro.Diagnostics;
using System.Diagnostics;

namespace JBSnorro.Collections.Bits;

public static class IFloatingPointBitReaderExtensions
{
    [DebuggerHidden]
    public static Half ReadHalf(this IBitReader reader, int bitCount, IFloatingPointBitReaderEncoding floatingPointEncoding)
    {
        if (bitCount > 16) throw new ArgumentOutOfRangeException(nameof(bitCount));
        return (Half)reader.ReadDouble(bitCount, floatingPointEncoding);
    }
    [DebuggerHidden]
    public static float ReadSingle(this IBitReader reader, int bitCount, IFloatingPointBitReaderEncoding floatingPointEncoding)
    {
        if (bitCount > 32) throw new ArgumentOutOfRangeException(nameof(bitCount));
        return (float)reader.ReadDouble(bitCount, floatingPointEncoding);
    }
    public static double ReadDouble(this IBitReader reader, int bitCount, IFloatingPointBitReaderEncoding floatingPointEncoding)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (bitCount < IFloatingPointBitReader.MIN_BIT_COUNT) throw new ArgumentOutOfRangeException(nameof(bitCount));
        if (!Enum.IsDefined(floatingPointEncoding)) throw new ArgumentOutOfRangeException(nameof(floatingPointEncoding));

        switch (floatingPointEncoding)
        {
            case IFloatingPointBitReaderEncoding.Default:
                return IFloatingPointBitReader.DefaultReadDouble(reader, bitCount);
            case IFloatingPointBitReaderEncoding.ULongLike:
                return (float)ULongLikeFloatingPointBitReader.ReadDouble(reader, bitCount);
            default:
                throw new DefaultSwitchCaseUnreachableException();
        }
    }
    [DebuggerHidden]
    public static IFloatingPointBitReader ToFloatingPointBitReader(this IBitReader bitReader, IFloatingPointBitReaderEncoding encoding)
    {
        return IFloatingPointBitReader.Create(bitReader, encoding);
    }
}
