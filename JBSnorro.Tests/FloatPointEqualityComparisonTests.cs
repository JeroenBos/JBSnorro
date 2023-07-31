using Microsoft.VisualStudio.TestTools.UnitTesting;
using JBSnorro;

namespace Tests.JBSnorro;

[TestClass]
public class FloatingPointApproximationTests
{
    [TestMethod]
    public void TestFloatApproximatelyEqual()
    {
        Assert.IsTrue(EqualityExtensions.ApproximatelyEquals(0, 0));
        Assert.IsFalse(EqualityExtensions.ApproximatelyEquals(0, 1));
        Assert.IsTrue(EqualityExtensions.ApproximatelyEquals(0.1, 0.1));
        Assert.IsFalse(EqualityExtensions.ApproximatelyEquals(0.1, 0));
        Assert.IsTrue(EqualityExtensions.ApproximatelyEquals(0.1, 0, tolerance: 0.11));


        Assert.IsTrue(EqualityExtensions.ApproximatelyEquals(double.NaN, double.NaN));
        Assert.IsTrue(EqualityExtensions.ApproximatelyEquals(double.PositiveInfinity, double.PositiveInfinity));
        Assert.IsTrue(EqualityExtensions.ApproximatelyEquals(double.NegativeInfinity, double.NegativeInfinity));
        Assert.IsFalse(EqualityExtensions.ApproximatelyEquals(double.NaN, 0));
        Assert.IsFalse(EqualityExtensions.ApproximatelyEquals(double.NaN, -1));
        Assert.IsFalse(EqualityExtensions.ApproximatelyEquals(1, double.NaN));
        Assert.IsFalse(EqualityExtensions.ApproximatelyEquals(double.PositiveInfinity, double.NaN));
        Assert.IsFalse(EqualityExtensions.ApproximatelyEquals(double.PositiveInfinity, double.NegativeInfinity));
        Assert.IsFalse(EqualityExtensions.ApproximatelyEquals(double.NegativeInfinity, double.PositiveInfinity));
    }
}
