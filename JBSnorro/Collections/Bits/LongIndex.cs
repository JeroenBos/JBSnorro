using System.Diagnostics.CodeAnalysis;
using JBSnorro.Diagnostics;

namespace JBSnorro.Collections.Bits;

/// <summary>
/// An <see cref="Index"/> with the capacity of <see cref="long"/>.
/// When from the start is always inclusive, and from the end is always exclusive.
/// </summary>
public readonly struct LongIndex
{
    private readonly long value;
    public bool IsFromEnd
    {
        get => value < 0;
    }
    public ulong Value
    {
        get
        {
            return value < 0 ? (ulong)~value : (ulong)value;
        }
    }

    public LongIndex(ulong value, bool isFromEnd = false)
    {
        Contract.Requires<ArgumentOutOfRangeException>(value <= long.MaxValue);

        unchecked
        {
            if (isFromEnd)
                this.value = (long)~value;
            else
                this.value = (long)value;
        }
    }
    public LongIndex(Index index) : this((ulong)index.Value, index.IsFromEnd)
    {
    }

    public static LongIndex FromStart(int value)
    {
        Contract.Requires<ArgumentOutOfRangeException>(value >= 0);

        return new LongIndex((ulong)value, false);
    }
    public static LongIndex FromEnd(int value)
    {
        Contract.Requires<ArgumentOutOfRangeException>(value >= 0);

        return new LongIndex((ulong)value, true);
    }
    public static LongIndex FromStart(ulong value)
    {
        return new LongIndex(value, false);
    }
    public static LongIndex FromEnd(ulong value)
    {
        return new LongIndex(value, true);
    }

    public ulong GetOffset(ulong length)
    {
        if (this.value >= 0)
        {
            return (ulong)this.value;
        }
        else
        {
            ulong fromEnd = (ulong)~this.value;
            if (fromEnd > length) throw new ArgumentOutOfRangeException(nameof(length));
            return length - fromEnd;
        }
    }
    /// <summary>
    /// Gets whether this index fits in a collection of the specified length.
    /// </summary>
    public bool Fits(ulong length)
    {
        if (this.value >= 0)
        {
            return (ulong)this.value <= length;
        }
        else
        {
            ulong fromEnd = (ulong)~this.value;
            return fromEnd <= length;
        }
    }

    public static explicit operator Index(LongIndex longIndex)
    {
        Contract.Requires<InvalidOperationException>(longIndex.Value <= int.MaxValue);

        return new Index((int)longIndex.Value, longIndex.IsFromEnd);
    }
    public static implicit operator LongIndex(Index index)
    {
        return new LongIndex(index);
    }
    public static explicit operator LongIndex(ulong index)
    {
        return new LongIndex(index);
    }
    public static implicit operator LongIndex(long index)
    {
        Contract.Requires(index >= 0);
        return new LongIndex((ulong)index);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is LongIndex longIndex && longIndex.value == this.value;
    }
    public override int GetHashCode()
    {
        return this.value.GetHashCode();
    }
    public override string ToString()
    {
        if (IsFromEnd)
        {
            return '^' + (~this.value).ToString();
        }
        return this.value.ToString();
    }
    public static bool operator ==(LongIndex left, LongIndex right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LongIndex left, LongIndex right)
    {
        return !(left == right);
    }
}
