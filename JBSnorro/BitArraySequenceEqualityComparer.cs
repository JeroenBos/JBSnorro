using System.Collections;

namespace JBSnorro;

public sealed class BitArraySequenceEqualityComparer : IEqualityComparer<BitArray>
{
    public readonly static BitArraySequenceEqualityComparer Instance = new BitArraySequenceEqualityComparer();

    private BitArraySequenceEqualityComparer() { }

    public bool Equals(BitArray? x, BitArray? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (ReferenceEquals(y, null))
            return false;

        return x != null
            && x.Length == y.Length
            && x.Cast<bool>().SequenceEqual(y.Cast<bool>());
    }

    public int GetHashCode(BitArray obj)
    {
        int result = 1;
        int index = 0;
        foreach (bool bit in obj)
        {
            result += ((bit ? 1 : 0) + index) * 17;
            index++;
        }
        return result;
    }
}
