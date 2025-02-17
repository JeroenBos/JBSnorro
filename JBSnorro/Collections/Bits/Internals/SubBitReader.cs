using JBSnorro.Diagnostics;

namespace JBSnorro.Collections.Bits.Internals;

/// <summary>
/// A bit reader that is a subsection of another bitreader. These will read in lock-step, 
/// </summary>
internal class SubBitReader : IBitReader
{
    private readonly IBitReader _base;
    private readonly ulong startPosition;
    private readonly ulong endPosition;
    private ulong remaining;

    public SubBitReader(IBitReader baseBitReader, ulong length)
    {
        Contract.Requires(baseBitReader is not null);
        Contract.Requires(length <= baseBitReader.RemainingLength);

        this._base = baseBitReader;
        this.startPosition = baseBitReader.Position;
        this.endPosition = this.startPosition + length;
        this.remaining = length;
    }
    private SubBitReader(IBitReader baseBitReader, ulong startPosition, ulong endPosition)
    {
        this._base = baseBitReader;
        this.startPosition = startPosition;
        this.endPosition = endPosition;
    }

    public ulong Length => endPosition - startPosition;
    /// <summary>
    /// When the base reader has passed the end position, an <see cref="OverflowException"/> is thrown, because there is no value to return that wouldn't mean something else valid.
    /// </summary>
    public ulong Position => checked(_base.Position - this.startPosition);
    public ulong RemainingLength => this.endPosition >= _base.Position ? this.endPosition - _base.Position : 0;

    public IBitReader Clone(LongIndex start, LongIndex end)
    {
        return new SubBitReader(_base, startPosition, endPosition) { remaining = this.remaining };
    }

    public ulong ReadUInt64(int bitCount = 64)
    {
        ulong result = _base.ReadUInt64(bitCount);
        remaining -= (uint)bitCount;
        return result;
    }

    public void Seek(ulong bitIndex)
    {
        Contract.Requires(bitIndex <= endPosition);

        _base.Seek(this.startPosition + bitIndex);
        this.remaining = endPosition - bitIndex;
    }
}
