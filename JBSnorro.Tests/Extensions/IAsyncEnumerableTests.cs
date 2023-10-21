using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Tests.JBSnorro.Extensions;

[TestClass]
public class IAsyncEnumerableSkipTests
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

[TestClass]
public class IAsyncEnumerableCreateTests
{
    [TestMethod]
    public async Task TestSemaphoreThatIsntCalledDoesntYield()
    {
        var test = Task.Delay(100);

        await foreach (var _ in IAsyncEnumerableExtensions.Create(out Action yield, TimeSpan.FromMilliseconds(300))())
        {
            throw new UnreachableException();
        }
        await test;

    }
    [TestMethod]
    public async Task TestSemaphoreIsCalledYields()
    {
        var enumerable = IAsyncEnumerableExtensions.Create(out Action yield, TimeSpan.FromMilliseconds(300));
        var test = Task.Run(async () =>
        {
            await Task.Delay(100);
            yield();
            await Task.Delay(100);
        });

        int i = 0;
        await foreach (var _ in enumerable())
        {
            i++;
        }
        await test;
        Contract.Assert(i == 1);
    }

    [TestMethod]
    public async Task TestSemaphoreIsCalledTwiceYieldsInCadence()
    {
        var enumerable = IAsyncEnumerableExtensions.Create(out Action yield, TimeSpan.FromMilliseconds(300));
        var arrange = Task.Run(async () =>
        {
            await Task.Delay(100);
            Console.WriteLine("yield");
            yield();
            await Task.Delay(100);
            Console.WriteLine("yield");
            yield();
            await Task.Delay(100);
        });

        int i = 0;
        var act = Task.Run(async () =>
        {
            await foreach (var _ in enumerable())
            {
                i++;
            }
        });

        var assert = Task.Run(async () =>
        {
            Console.WriteLine("awaiting 110");
            await Task.Delay(150);
            Console.WriteLine("check i == 1: {0}", i);
            Contract.Assert(i == 1, $"Was {i}");
            await Task.Delay(150);
            Console.WriteLine("check i == 2: {0}", i);
            Contract.Assert(i == 2, $"Was {i}");
        });

        await Task.WhenAll(arrange, act, assert);
    }
}