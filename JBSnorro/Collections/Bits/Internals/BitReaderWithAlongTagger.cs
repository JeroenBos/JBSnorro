namespace JBSnorro.Collections.Bits.Internals;

/// <summary>
/// A bit reader that reads floating point numbers assuming they're particularly encoded.
/// </summary>
internal class BitReaderWithAlongTagger : BitReader
{
    private readonly BitReader alongTagger;

    public BitReaderWithAlongTagger(BitArray data, ulong position, ulong length, BitReader alongTagger)
        : base(data, position, length)
    {
        this.alongTagger = alongTagger;
    }

    protected internal override ulong current
    {
        get
        {
            return base.current;
        }
        set
        {
            unchecked
            {
                var delta = value - current;
                if (delta != 0)
                {
                    this.current = value;
                    alongTagger.current += delta;
                }
            }
        }
    }

    public override IBitReader Clone()
    {
        return this[this.RemainingLength, false];
    }
}
