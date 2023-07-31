using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// Represents a dimensional quantity in one discrete finite-length dimension. The start of the dimension is closed and the end open.
/// </summary>
/// <param name="Value">The position of this quantity in this dimension. </param>
/// <param name="Length">The length of the dimension. Must be nonnegative.</param>
/// <param name="Start">The start of the dimension. </param>
public record struct OneDimensionalDiscreteQuantity(int Value, int Length, int Start = 0);
/// <summary>
/// Represents a dimensional quantity in one continuous dimension. The start of the dimension is closed and the end open (although not very relevant practically for continuous quantities).
/// </summary>
/// <param name="Value">The position of this quantity in this dimension. </param>
/// <param name="Length">The length of the dimension. Must be nonnegative. Can be infinite. </param>
/// <param name="Start">The start of the dimension. </param>
public record struct OneDimensionalContinuousQuantity(float Value, float Length, float Start = 0);

public interface IDimensionfulContinuousFunction<TResult>
{
    TResult Invoke(OneDimensionalContinuousQuantity arg);
}
public interface IDimensionfulContinuousFunction : IDimensionfulContinuousFunction<float>
{
}

public interface IDimensionfulDiscreteFunction<TResult>
{
    TResult Invoke(OneDimensionalDiscreteQuantity arg);


    [DebuggerHidden]
    public Func<int, TResult> On(int length, int start = 0)
    {
        return [DebuggerHidden] (arg) => this.Invoke(new OneDimensionalDiscreteQuantity(arg, length, start));
    }
    public static IDimensionfulDiscreteFunction<TResult> Create(IDimensionfulDiscreteFunction<TResult> f)
    {
        return new DimensionfulDiscreteFunctionImpl(f);
    }
    //public static IDimensionfulDiscreteFunction Create(Func<OneDimensionalDiscreteQuantity, int> f) => new DimensionfulDiscreteFunctionImpl(new DimensionfulDiscreteFunction(f));
    private record DimensionfulDiscreteFunctionImpl(IDimensionfulDiscreteFunction<TResult> f) : IDimensionfulDiscreteFunction<TResult>
    {
        TResult IDimensionfulDiscreteFunction<TResult>.Invoke(OneDimensionalDiscreteQuantity arg) => f.Invoke(arg);
    }
}
public interface IDimensionfulDiscreteFunction : IDimensionfulDiscreteFunction<float>
{

}

public static class DimensionalFunctionExtensions
{
    public static OneDimensionalContinuousQuantity ToContinuous(this OneDimensionalDiscreteQuantity discreteQuantity)
    {
        return new OneDimensionalContinuousQuantity(discreteQuantity.Value, discreteQuantity.Length, discreteQuantity.Start);
    }
    public static Func<OneDimensionalDiscreteQuantity, TResult> Map<TResult>(this IDimensionfulDiscreteFunction f, Func<OneDimensionalContinuousQuantity, TResult> map, float mapDomainLength, float mapDomainStart = 0)
    {
        return g;
        TResult g(OneDimensionalDiscreteQuantity arg)
        {
            var returnedValue = f.Invoke(arg);
            var returnedValueWithUnits = new OneDimensionalContinuousQuantity(returnedValue, mapDomainLength, mapDomainStart);
            return map(returnedValueWithUnits);
        }
    }
    public static Func<OneDimensionalContinuousQuantity, TResult> Map<TResult>(this IDimensionfulContinuousFunction f, Func<OneDimensionalContinuousQuantity, TResult> map, float mapDomainLength, float mapDomainStart = 0)
    {
        return g;
        TResult g(OneDimensionalContinuousQuantity arg)
        {
            var returnedValue = f.Invoke(arg);
            var returnedValueWithUnits = new OneDimensionalContinuousQuantity(returnedValue, mapDomainLength, mapDomainStart);
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
    //public static DimensionfulFunction<TResult> Map<TResult>(this DimensionfulFunction f, Func<int, TResult> map)
    //{
    //    return g;
    //    TResult g(OneDimensionalDiscreteQuantity arg)
    //    {
    //        var returnedValue = f(arg);
    //        return map(returnedValue);
    //    }
    //}
    //public static DimensionfulFunction Compose(this DimensionfulFunction f, Func<int, int> map)
    //{
    //    return g;
    //    int g(OneDimensionalDiscreteQuantity arg)
    //    {
    //        var mappedStart = map(arg.Start);
    //        var mappedEnd = map(arg.Start + arg.Length);
    //        var mappedLength = mappedEnd - mappedStart;
    //        var mappedValue = map(arg.Value);

    //        var mapped = new OneDimensionalDiscreteQuantity(mappedValue, mappedLength, mappedStart);
    //        return f(mapped);
    //    }
    //}
}