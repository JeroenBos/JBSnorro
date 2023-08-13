using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.JBSnorro.Extensions;

[TestClass]
public class IAsyncEnumerableTests
{
    async IAsyncEnumerable<int> Create(int[] items)
    {
        await Task.Delay(1);
        foreach (var item in items)
        {
            yield return item;
            await Task.Delay(1);
        }
    }
    [TestMethod]
    public async Task TestSkipZero()
    {
        var items = Create(new int[] { 1, 2, 3 });
        var expected = new int[] { 1, 2, 3 };

        var actual = await items.Skip(0).ToList();

        Contract.AssertSequenceEqual(actual, expected);
    }
    [TestMethod]
    public async Task TestSkipOne()
    {
        var items = Create(new int[] { 1, 2, 3 });
        var expected = new int[] { 2, 3 };

        var actual = await items.Skip(1).ToList();

        Contract.AssertSequenceEqual(actual, expected);
    }
    [TestMethod]
    public async Task TestSkipTwo()
    {
        var items = Create(new int[] { 1, 2, 3 });
        var expected = new int[] { 3 };

        var actual = await items.Skip(2).ToList();

        Contract.AssertSequenceEqual(actual, expected);
    }
    [TestMethod]
    public async Task TestSkipAll()
    {
        var items = Create(new int[] { 1, 2, 3 });
        var expected = new int[] { };

        var actual = await items.Skip(3).ToList();

        Contract.AssertSequenceEqual(actual, expected);
    }
    [TestMethod]
    public async Task TestSkipMoreThanPresent()
    {
        var items = Create(new int[] { 1, 2, 3 });
        var expected = new int[] { };

        var actual = await items.Skip(4).ToList();

        Contract.AssertSequenceEqual(actual, expected);
    }
}
