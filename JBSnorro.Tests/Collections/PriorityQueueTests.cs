using JBSnorro.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JBSnorro.Tests.Collections;

[TestClass]
public class PriorityQueueTests
{
    [DataTestMethod]
    public void CapacitylessCacheContainsAddedElement()
    {
        var cache = Cache<int, int>.Create(i => i);
        var one = cache[1];

        Assert.IsTrue(cache.TryGetValue(1, out var _));
    }

    [DataTestMethod]
    public void CapacitylessCacheDoesntContainsNotAddedElement()
    {
        var cache = Cache<int, int>.Create(i => i);
        var one = cache[1];

        Assert.IsFalse(cache.TryGetValue(2, out var _));
    }
    [DataTestMethod]
    public void CapacityfulCacheContainsAddedElement()
    {
        var cache = Cache<int, int>.Create(i => i, 2);
        var one = cache[1];

        Assert.IsTrue(cache.TryGetValue(1, out var _));
    }

    [DataTestMethod]
    public void CapacityfulCacheDoesntContainsNotAddedElement()
    {
        var cache = Cache<int, int>.Create(i => i, 2);
        var one = cache[1];

        Assert.IsFalse(cache.TryGetValue(2, out var _));
    }
    [DataTestMethod]
    public void CacheContainsAddedElementsOnOverflow()
    {
        var cache = Cache<int, int>.Create(i => i, 1);
        var one = cache[1];
        var two = cache[2];

        Assert.IsTrue(cache.TryGetValue(2, out var _));
    }

    [TestMethod]
    public void OverflowingCapacityKicksOutFirstAdded()
    {
        var cache = Cache<int, int>.Create(i => i, 2);
        var one = cache[1];
        var two = cache[2];
        var three = cache[3];

        Assert.IsFalse(cache.TryGetValue(1, out var _));
    }

    [TestMethod]
    public void OverflowingCapacityKicksOutLastTouched()
    {
        var cache = Cache<int, int>.Create(i => i, 2);
        var one = cache[1];
        var two = cache[2];
        one = cache[1];
        var three = cache[3];


        Assert.IsFalse(cache.TryGetValue(2, out var _));
    }
}
