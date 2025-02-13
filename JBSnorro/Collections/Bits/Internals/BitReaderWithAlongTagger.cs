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

    public override IBitReader Clone(LongIndex start, LongIndex end)
    {
        // clone means to decouple from the current one. And the current one is coupled to the base one, so we must decouple from that one as well
        // so then we can just clone that one:
        return alongTagger.Clone(
            start: new LongIndex(this.startOffset + start.GetOffset(this.Length)),
            end: new LongIndex(this.startOffset + end.GetOffset(this.Length))
        );
    }
}
