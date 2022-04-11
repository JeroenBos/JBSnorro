#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro.Extensions;

public static class RandomExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="drawCount"></param>
    /// <param name="max"> Exlusive. </param>
    /// <exception cref="NotImplementedException"></exception>
    public static ulong[] GenerateUniqueRandomNumbers(this Random random, int drawCount, int max)
    {
        var result = new List<ulong>(capacity: drawCount);

        // this approach is very flawed
        // int m = max;
        // for (int i = 0; i < drawCount; i++)
        // {
        //     var next = (ulong)random.Next(0, m);
        //     var offset = (ulong)result.Count(t => t <= next);
        //     result.Add(next + offset);
        //     m--;
        // }


        var list = new int[max];
        for (int i = 0; i < max; i++)
            list[i] = i;
        random.Shuffle(list);

        return list.Take(drawCount).Select(i => (ulong)i).ToArray();
    }
    public static float Normal(this Random random, float average, float standardDevation)
    {
        // from https://stackoverflow.com/a/218600/308451
        double u1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
        double result = average + standardDevation * randStdNormal; // random normal(mean,stdDev^2)
        return (float)result;
    }
    public static int[] Many(this Random random, int count, int min, int max)
    {
        int[] result = new int[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = random.Next(min, max);
        }
        return result;
    }
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
    public static int[] ManySorted(this Random random, int count, int min, int max)
    {
        var result = random.Many(count, min, max);
        Array.Sort(result);
        return result;
    }
    public static ulong[] ManySorted(this Random random, int count, ulong min, ulong max)
    {
        var result = random.Many(count, min, max);
        Array.Sort(result);
        return result;
    }

    public static void Shuffle<T>(this Random random, IList<T> list)
    {
        for (int n = list.Count - 1; n > 1; n--)
        {
            int k = random.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
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
