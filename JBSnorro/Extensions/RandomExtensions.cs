#nullable enable
using System.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using JBSnorro.Text.Json;

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
    /// <param name="max">Exclusive</param>
    public static ulong[] ManyUnique(this Random random, int drawCount, ulong max, ulong min = 0)
    {
        if (drawCount == 0)
        {
            return Array.Empty<ulong>();
        }

        if (max - min > 10UL * (ulong)drawCount)
        {
            var result = new HashSet<ulong>();
            while (result.Count != drawCount)
            {
                var r = random.NextUInt64(min, max);
                if (!result.Contains(r))
                    result.Add(r);
            }
            return result.ToArray();
        }


        var list = new int[max];
        for (int i = 0; i < (int)max; i++)
            list[i] = i;
        list.Shuffle(random);

        return list.Take(drawCount).Select(i => (ulong)i + min).ToArray();
    }
    /// <param name="max">Exclusive</param>
    public static ulong[] ManyUnique(this Random random, int drawCount, ulong max, ulong min = 0, params ulong[] exclusions)
    {
        if (exclusions == null || exclusions.Length == 0)
            return ManyUnique(random, drawCount, max, min);
        if (drawCount < 0)
            throw new ArgumentOutOfRangeException(nameof(drawCount));
        if (min < 0)
            throw new ArgumentOutOfRangeException(nameof(min));
        if (min >= max)
            throw new ArgumentOutOfRangeException(nameof(min));
        if (max - min - (ulong)exclusions.Length < (ulong)drawCount)
            throw new ArgumentException("Too many excluded", nameof(exclusions));
        Array.Sort(exclusions);
        for (int i = 0; i < exclusions.Length - 1; i++)
        {
            if (exclusions[i] == exclusions[i + 1])
                throw new ArgumentException("Duplicate entries not allowed", nameof(exclusions));
            if (exclusions[0] < min)
                throw new ArgumentOutOfRangeException("An exclusion is below min", nameof(exclusions));
            if (exclusions[^-1] > max)
                throw new ArgumentOutOfRangeException("An exclusion is above max", nameof(exclusions));
            if (exclusions[^-1] > max - (ulong)exclusions.Length)
                throw new NotImplementedException("Algorithm wouldn't work");
        }

        return random.ManyUnique(drawCount, max - (ulong)exclusions.Length, min)
              .Map((ulong i) =>
              {
                  int index = exclusions.IndexOf(i);
                  if (index == -1)
                      return i;
                  return max - 1 - (ulong)index /*"- 1" because max is exclusive*/;
              });
    }

    public static bool NextBoolean(this Random random)
    {
        return random.Next(2) == 0;
    }
    public static ulong NextUInt64(this Random random)
    {
        var a = (ulong)random.NextInt64(0, (long)uint.MaxValue + 1);
        var b = (ulong)random.NextInt64(0, (long)uint.MaxValue + 1);
        return a | (b << 32);
    }
    /// <param name="maxValue">Exclusive.</param>
    public static ulong NextUInt64(this Random random, ulong maxValue)
    {
        if (maxValue > long.MaxValue) throw new NotImplementedException("maxValue > long.MaxValue");

        return (ulong)random.NextInt64((long)maxValue);
    }
    /// <param name="maxValue">Exclusive</param>
    public static ulong NextUInt64(this Random random, ulong minValue, ulong maxValue)
    {
        if (maxValue > long.MaxValue) throw new NotImplementedException("maxValue > long.MaxValue");
        if (minValue > maxValue) throw new NotImplementedException("minValue > maxValue");

        return minValue + (ulong)random.NextInt64((long)(maxValue - minValue));
    }

    /// <summary>
    /// Draws a number in [0, length) with a linearly decreasing (normalized) probability function.
    /// <seealso cref="https://stats.stackexchange.com/a/171631/176526"/>
    /// </summary>
    public static int DrawNumberWithLinearDecreasingDistribution(this Random random, int length)
    {
        if (random is null) throw new ArgumentNullException(nameof(random));
        if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));

        float u = random.NextSingle();
        return toIndex(u, length);

        static double draw(float u)
        {
            const int alpha = -1;
            return (Math.Sqrt(alpha * alpha - 2 * alpha + 4 * alpha * u + 1) - 1) / alpha;
        }
        static int toIndex(float u, int size)
        {
            var p = (draw(u) + 1) / 2; // shift 1 to the right in domain, so now that's from 0 to 1
            var result = p * size; // scale it by population size
            return (int)result;
            // A number of random members must be added to each round, until it's not necessary anymore. Am I doing that?
        }
    }
    /// <summary>
    /// Draws two distinct numbers in [0, length) with a linearly decreasing (normalized) probability function.
    /// <seealso cref="https://stats.stackexchange.com/a/171631/176526"/>
    /// </summary>
    public static (int, int) DrawTwoUniqueNumbersWithLinearDecreasingDistribution(this Random random, int length)
    {
        if (random is null) throw new ArgumentNullException(nameof(random));
        if (length < 2) throw new ArgumentOutOfRangeException(nameof(length));

        int number1 = random.DrawNumberWithLinearDecreasingDistribution(length);

        int number2 = number1;
        while (number2 == number1)
        {
            number1 = random.DrawNumberWithLinearDecreasingDistribution(length);
        }
        return (number1, number2);
    }

    /// <summary>
    /// Generates normally distributed numbers.
    /// </summary>
    /// <param name="r"></param>
    /// <param name = "mu">Mean of the distribution</param>
    /// <param name = "σ">Standard deviation</param>
    /// <seealso cref="https://stackoverflow.com/a/15556411/308451"/>
    public static double NextGaussian(this Random r, double μ = 0, double σ = 1)
    {
        var u1 = r.NextDouble();
        var u2 = r.NextDouble();

        var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(1 - u1)) * Math.Sin(2.0 * Math.PI * u2);

        var rand_normal = μ + σ * rand_std_normal;

        return rand_normal;
    }
    public static double NextClampedGaussian(this Random r, double μ = 0, double σ = 1, double min = 0, double max = 1)
    {
        var variable = r.NextGaussian(μ: μ, σ: σ);
        if (variable < min)
            return min;
        if (variable > max)
            return max;
        return variable;
    }
    public static int NextClampedGaussian(this Random r, double μ = 0, double σ = 1, int min = 0, int max = 1)
    {
        var variable = r.NextGaussian(μ: μ, σ: σ);
        if (variable < min)
            return min;
        if (variable > max)
            return max;
        return (int)variable;
    }
    public static ulong NextClampedGaussian(this Random r, double μ = 0, double σ = 1, ulong min = 0, ulong max = 1)
    {
        var variable = r.NextGaussian(μ: μ, σ: σ);
        if (variable < min)
            return min;
        if (variable > max)
            return max;
        return (ulong)variable;
    }

    /// <summary>
    /// Gets a specified number of relatively commensurate parts of a specified length, adding up to that length.
    /// </summary>
    /// <param name="random"></param>
    /// <param name="count">The number of points to place on the line.</param>
    /// <param name="totalLength">The length of the line, inclusive. </param>
    /// <returns>the distances of each subline.</returns>
    public static IEnumerable<ulong> DrawRoughlyCommensurateLengths(this Random random, int count, ulong totalLength)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (totalLength < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0)
        {
            yield return totalLength;
            yield break;
        }

        // create a junk length that is roughly the same between all cistrons
        var averageDistance = totalLength / (double)(count + 1); // + 1 because there is one more than sequences, at the end
        var distance = random.NextClampedGaussian(μ: averageDistance, σ: averageDistance, min: 0, max: totalLength);
        yield return distance;

        var remainingLength = totalLength - distance;
        foreach (var otherJunkLength in random.DrawRoughlyCommensurateLengths(count - 1, remainingLength))
        {
            yield return otherJunkLength;
        }
    }
    /// <summary>
    /// Gets a specified number of relatively equidistantly dispersed points on a line of the specified length.
    /// </summary>
    /// <param name="random"></param>
    /// <param name="count">The number of points to place on the line.</param>
    /// <param name="totalLength">The length of the line, inclusive. </param>
    /// <returns>the locations of each point.</returns>
    public static IEnumerable<double> DrawRelativelyEquidistantlyInterspersedPoints(this Random random, int count, ulong totalLength)
    {
        int pointCount = count;
        int lineCount = pointCount + 1;

        return random.DrawRoughlyCommensurateLengths(lineCount, totalLength)
                     .Take(count) // skips the line to the end
                     .Scan((a, b) => a + b, 0d);
    }

    /// <summary>
    /// Draws an element at random from the specified collection.
    /// </summary>
    public static T Draw<T>(this Random random, IReadOnlyList<T> collection)
    {
        return collection[random.Next(collection.Count)];
    }

    public static JsonConverter<Random> JsonConverter { get; } = new JsonConverterBy2<Random, RandomState>(RandomState.ToRandom, RandomState.GetState!);
    public static RandomState ToState(this Random random)
    {
        return RandomState.GetState(random);
    }
    public static string Hash(this Random random)
    {
        return string.Format("{0:X8}", random.ToState().GetHashCode());
    }

    /// <summary>
    /// <seealso cref="https://stackoverflow.com/a/71812953/308451"/>
    /// </summary>
    public struct RandomState
    {
        public static Random ToRandom(RandomState state)
        {
            return state.ToRandom();
        }
        static RandomState()
        {
            ImplInfo = typeof(Random).GetField("_impl", BindingFlags.Instance | BindingFlags.NonPublic)!;
            PrngInfo = Type.GetType(Net5CompatSeedImplName)!.GetField("_prng", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Type compatPrngType = Type.GetType(CompatPrngName)!;
            seedArrayInfo = compatPrngType.GetField(SeedArrayInfoName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            inextInfo = compatPrngType.GetField(InextInfoName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            inextpInfo = compatPrngType.GetField(InextpInfoName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        }
        internal const string CompatPrngName = "System.Random+CompatPrng";
        internal const string Net5CompatSeedImplName = "System.Random+Net5CompatSeedImpl";
        internal const string SeedArrayInfoName = "_seedArray";
        internal const string InextInfoName = "_inext";
        internal const string InextpInfoName = "_inextp";
        private static readonly FieldInfo ImplInfo;
        private static readonly FieldInfo PrngInfo;
        private static readonly FieldInfo seedArrayInfo;
        private static readonly FieldInfo inextInfo;
        private static readonly FieldInfo inextpInfo;

        public int[] seedState { get; set; }
        public int inext { get; set; }
        public int inextp { get; set; }
        public static RandomState GetState(Random random)
        {
            object o = GetCompatPrng(random);
            RandomState state = new RandomState();
            state.seedState = (int[])seedArrayInfo.GetValue(o)!;
            state.inext = (int)inextInfo.GetValue(o)!;
            state.inextp = (int)inextpInfo.GetValue(o)!;
            return state;

        }
        public Random ToRandom()
        {
            var result = new Random(0); // seed value is important, but presence is important as it selects a different implementation
            SetState(result, this);
            return result;
        }

        //Random > Impl > CompatPrng
        internal static object GetImpl(Random random) => ImplInfo.GetValueDirect(__makeref(random))!;
        internal static object GetCompatPrng(object impl) => PrngInfo.GetValueDirect(__makeref(impl))!;
        internal static object GetCompatPrng(Random random)
        {
            object impl = GetImpl(random);
            return PrngInfo.GetValueDirect(__makeref(impl))!;
        }
        internal static void SetState(Random random, RandomState state)
        {
            object impl = GetImpl(random);

            object prng = PrngInfo.GetValue(impl)!;

            seedArrayInfo.SetValue(prng, state.seedState);
            inextInfo.SetValue(prng, state.inext);
            inextpInfo.SetValue(prng, state.inextp);

            PrngInfo.SetValue(impl, prng);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return inext * 13 + inextp * 11 + SequenceEqualityComparer<int>.GetHashCode(seedState);
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.Append("  inext=");
            builder.Append(inext);
            builder.AppendLine(",");
            builder.Append("  inextp=");
            builder.Append(inextp);
            builder.AppendLine(",");
            builder.Append("  data=[\n");
            for (int i = 0; i < this.seedState.Length; i++)
            {
                builder.AppendLine($"    {this.seedState[i]},");
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}