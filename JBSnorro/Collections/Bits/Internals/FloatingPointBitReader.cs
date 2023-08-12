namespace JBSnorro.Collections.Bits.Internals;

internal class FloatingPointBitReader : IFloatingPointBitReader
{
    private readonly ReadDoubleDelegate readDouble;
    public IBitReader Reader { get; }

    public FloatingPointBitReader(IBitReader reader, ReadDoubleDelegate readDouble)
    {
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.readDouble = readDouble ?? throw new ArgumentNullException(nameof(readDouble));
    }

    public double ReadDouble(int bitCount = 64)
    {
        return readDouble(Reader, bitCount);
    }
    IBitReader IBitReader.Clone()
    {
        return new FloatingPointBitReader(Reader.Clone(), readDouble);
    }
}

