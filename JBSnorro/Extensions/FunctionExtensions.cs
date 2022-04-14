using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro;

public static class FunctionExtensions
{
    public static Func<T, TResult> Map<T, U, TResult>(this Func<T, U> f, Func<U, TResult> map)
    {
        return x => map(f(x));
    }
    public static Func<T, U, TResult> Map<T, U, V, TResult>(this Func<T, U, V> f, Func<V, TResult> map)
    {
        return (t, u) => map(f(t, u));
    }
    public static Func<T, U, V, TResult> Map<T, U, V, W, TResult>(this Func<T, U, V, W> f, Func<W, TResult> map)
    {
        return (t, u, v) => map(f(t, u, v));
    }
    public static Func<T, V> Compose<T, U, V>(this Func<U, V> f, Func<T, U> innerFunction)
    {
        return x => f(innerFunction(x));
    }
}


/// <summary>
/// Represents a dimensional quantity in one discrete finite-length dimension.
/// </summary>
/// <param name="Value">The position of this quantity in this dimension. </param>
/// <param name="Length">The length of the dimension. Must be nonnegative. </param>
/// <param name="Start">The start of the dimension. </param>
public record struct OneDimensionalDiscreteQuantity(int Value, int Length, int Start = 0);
/// <summary>
/// Represents a dimensional quantity in one continuous dimension.
/// </summary>
/// <param name="Value">The position of this quantity in this dimension. </param>
/// <param name="Length">The length of the dimension. Must be nonnegative. Can be infinite. </param>
/// <param name="Start">The start of the dimension. </param>
public record struct OneDimensionalContinuousQuantity(float Value, float Length, float Start = 0);

public delegate int DimensionfulDiscreteFunction(OneDimensionalDiscreteQuantity arg);
public delegate TResult DimensionfulFunction<TResult>(OneDimensionalDiscreteQuantity arg);
public delegate float DimensionfulContinuousFunction(OneDimensionalContinuousQuantity arg);


public static class DimensionalFunctionExtensions
{
    public static DimensionfulDiscreteFunction Map(this DimensionfulDiscreteFunction f, Func<OneDimensionalDiscreteQuantity, int> map, int domainLength, int domainStart = 0)
    {
        return g;
        int g(OneDimensionalDiscreteQuantity arg)
        {
            var returnedValue = f(arg);
            var returnedValueWithUnits = new OneDimensionalDiscreteQuantity(returnedValue, domainLength, domainStart);
            return map(returnedValueWithUnits);
        }
    }
    //public static DimensionfulDiscreteFunction Map(this DimensionfulDiscreteFunction f, Func<OneDimensionalDiscreteQuantity, OneDimensionalDiscreteQuantity> map)
    //{
    //    return g;
    //    int g(OneDimensionalDiscreteQuantity arg)
    //    {
    //        var returnedValueWithUnits = f(arg);
    //        return map(returnedValueWithUnits).Value;
    //    }
    //}
    public static DimensionfulFunction<TResult> Map<TResult>(this DimensionfulDiscreteFunction f, Func<int, TResult> map)
    {
        return g;
        TResult g(OneDimensionalDiscreteQuantity arg)
        {
            var returnedValue = f(arg);
            return map(returnedValue);
        }
    }
    public static DimensionfulDiscreteFunction Compose(this DimensionfulDiscreteFunction f, Func<int, int> map)
    {
        return g;
        int g(OneDimensionalDiscreteQuantity arg)
        {
            var mappedStart = map(arg.Start);
            var mappedEnd = map(arg.Start + arg.Length);
            var mappedLength = mappedEnd - mappedStart;
            var mappedValue = map(arg.Value);

            var mapped = new OneDimensionalDiscreteQuantity(mappedValue, mappedLength, mappedStart);
            return f(mapped);
        }
    }
}