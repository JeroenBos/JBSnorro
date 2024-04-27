using JBSnorro.Diagnostics;

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
        Contract.Requires(alongTagger is not null);
#if DEBUG
        var set = new HashSet<BitReader>();
        for (var a = alongTagger; a != null; a = (a as BitReaderWithAlongTagger)?.alongTagger)
        {
            if (set.Contains(a))
            {
                throw new ArgumentException("Infinite loop");
            }
            set.Add(a);
        }
#endif

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
            if (this.alongTagger is null)
            {
                // we're called from the base constructor
                base.current = value;
                return;
            }
            unchecked
            {
                var delta = value - current;
                if (delta != 0)
                {
                    base.current = value;
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
