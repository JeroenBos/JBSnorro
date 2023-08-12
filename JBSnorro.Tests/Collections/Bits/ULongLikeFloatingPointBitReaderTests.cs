using JBSnorro;
using JBSnorro.Collections.Bits;
using JBSnorro.Collections.Bits.Internals;
using JBSnorro.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Collections.Bits;

[TestClass]
public class ULongLikeFloatingPointBitReaderTests : IFloatingPointBitReaderTests
{
    public override IFloatingPointBitReader CreateFloatingPointBitReader(BitArray bitArray)
    {
        return bitArray.ToBitReader(IFloatingPointBitReaderEncoding.ULongLike);
    }

    // other tests are inherited
}
