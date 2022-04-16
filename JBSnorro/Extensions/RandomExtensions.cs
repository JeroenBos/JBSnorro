#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Extensions;

public static class RandomExtensions
{
    public static float Normal(this Random random, float average, float standardDevation)
    {
        // from https://stackoverflow.com/a/218600/308451
        double u1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
        double result = average + standardDevation * randStdNormal; // random normal(mean,stdDev^2)
        return (float)result;
    }
    /// <param name="max">Exclusive.</param>
    public static int[] Many(this Random random, int count, int min, int max)
    {
        int[] result = new int[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = random.Next(min, max);
        }
        return result;
    }
    /// <param name="max">Exclusive.</param>
    public static ulong[] Many(this Random random, int count, ulong min, ulong max)
    {
        ulong[] result = new ulong[count];
        for (int i = 0; i < count; i++)
        {
            checked
            {
                result[i] = (ulong)random.Next((int)min, (int)max);
            }
        }
        return result;
    }
    /// <param name="max">Exclusive.</param>
    public static int[] ManySorted(this Random random, int count, int min, int max)
    {
        var result = random.Many(count, min, max);
        Array.Sort(result);
        return result;
    }
    /// <param name="max">Exclusive.</param>
    public static ulong[] ManySorted(this Random random, int count, ulong min, ulong max)
    {
        var result = random.Many(count, min, max);
        Array.Sort(result);
        return result;
    }
    /// <summary>
    /// Generates many unique random numbers. Returned in random order.
    /// </summary>
    public static ulong[] ManyUnique(this Random random, int drawCount, int max, int min = 0)
    {
        // PERF: if max is way larger than drawCount, I'm sure there's a more efficient implementation

        var list = new int[max];
        for (int i = 0; i < max; i++)
            list[i] = i;
        list.Shuffle(random);

        return list.Take(drawCount).Select(i => (ulong)(i + min)).ToArray();
    }

    public static ulong NextUInt64(this Random random)
    {
         var a = (ulong)random.NextInt64(0, (long)uint.MaxValue + 1);
         var b = (ulong)random.NextInt64(0, (long)uint.MaxValue + 1);
         return a | (b >> 32);
    }
    /// <param name="maxValue">Exclusive.</param>
    public static ulong NextUInt64(this Random random, ulong maxValue)
    {
        if (maxValue > long.MaxValue) throw new NotImplementedException("maxValue > long.MaxValue");

        return (ulong)random.NextInt64((long)maxValue);
    }
}
