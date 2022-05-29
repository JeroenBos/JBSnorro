using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace JBSnorro.Tests;

[TestClass]
public class FloatingPointApproximationTests
{
    [TestMethod]
    public void TestFloatApproximatelyEqual()
    {
        Assert.IsTrue(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(0, 0));
        Assert.IsFalse(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(0, 1));
        Assert.IsTrue(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(0.1, 0.1));
        Assert.IsFalse(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(0.1, 0));
        Assert.IsTrue(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(0.1, 0, tolerance: 0.11));


        Assert.IsTrue(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(double.NaN, double.NaN));
        Assert.IsTrue(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(double.PositiveInfinity, double.PositiveInfinity));
        Assert.IsTrue(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(double.NegativeInfinity, double.NegativeInfinity));
        Assert.IsFalse(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(double.NaN, 0));
        Assert.IsFalse(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(double.NaN, -1));
        Assert.IsFalse(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(1, double.NaN));
        Assert.IsFalse(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(double.PositiveInfinity, double.NaN));
        Assert.IsFalse(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(double.PositiveInfinity, double.NegativeInfinity));
        Assert.IsFalse(FloatingTypeEqualityComparisonHelper.ApproximatelyEquals(double.NegativeInfinity, double.PositiveInfinity));
    }
}
