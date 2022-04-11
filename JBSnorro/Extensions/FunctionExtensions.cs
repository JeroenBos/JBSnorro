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
