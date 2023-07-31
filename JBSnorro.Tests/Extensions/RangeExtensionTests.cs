using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Extensions;

[TestClass] 
public class RangeExtensionTests

{
    [TestMethod]
    public void Test_subrange_not_from_end()
    {
        var result = RangeExtensions.SubRange(0..10, 1..8);
        var (offset, length) = result.GetOffsetAndLength(10);
        Contract.Assert(offset == 1);
        Contract.Assert(length == 7);
    }

    [TestMethod]
    public void Test_subrange_start_from_end()
    {
        var result = RangeExtensions.SubRange(0..10, ^7..8);
        var (offset, length) = result.GetOffsetAndLength(10);
        Contract.Assert(offset == 3);
        Contract.Assert(length == 5);
    }

    [TestMethod]
    public void Test_subrange_end_from_end()
    {
        var result = RangeExtensions.SubRange(0..10, 3..^2);
        var (offset, length) = result.GetOffsetAndLength(10);
        Contract.Assert(offset == 3);
        Contract.Assert(length == 5);
    }
    [TestMethod]
    public void Test_subrange_range_end_from_end()
    {
        var result = RangeExtensions.SubRange(0..^1, 3..^2);
        var (offset, length) = result.GetOffsetAndLength(10);
        Contract.Assert(offset == 3);
        Contract.Assert(length == 4);
    }
    [TestMethod]
    public void Test_subrange_range_start_from_end()
    {
        var result = RangeExtensions.SubRange(^9..10, 3..^2);
        var (offset, length) = result.GetOffsetAndLength(10);
        Contract.Assert(offset == 4);
        Contract.Assert(length == 4);
    }
    [TestMethod]
    public void Test_subrange_range_end_and_start_from_end()
    {
        var result = RangeExtensions.SubRange(0..^1, ^7..8);
        var (offset, length) = result.GetOffsetAndLength(10);
        Contract.Assert(offset == 2);
        Contract.Assert(length == 6);
    }
    [TestMethod]
    public void Test_subrange_range_start_and_start_from_end()
    {
        var result = RangeExtensions.SubRange(^9..10, ^6..8);
        var (offset, length) = result.GetOffsetAndLength(10);
        Contract.Assert(offset == 4);
        Contract.Assert(length == 5);
    }
}
