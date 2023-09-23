using JBSnorro.Collections.Bits;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Collections.Bits;

[TestClass]
public class ULongLikeFloatingPointBitReaderTests : IFloatingPointBitReaderTests
{
    public override IFloatingPointBitReader CreateFloatingPointBitReader(BitArray bitArray)
    {
        return bitArray.ToBitReader(IFloatingPointBitReaderEncoding.ULongLike);
    }
    public override IFloatingPointBitReaderEncoding Encoding => IFloatingPointBitReaderEncoding.ULongLike;
    // other tests are inherited
}
