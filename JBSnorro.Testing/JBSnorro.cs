using System.Diagnostics;

static class JBSnorro16
{
    /// <summary>
    /// Casts the entire array from <typeparamref name="T"/> to <typeparamref name="TResult"/>.
    /// </summary>
    /// <param name="array">The source array.</param>
    public static TResult[] CastAll<T, TResult>(this T[] array) where TResult : class
    {
        return Array.ConvertAll<T, TResult>(array, element => (TResult)(object)element!);
    }

    /// <summary>
    /// Gets an IEnumerable that throws on enumeration.
    /// </summary>
    public static IEnumerable<T> EnumerableExtensions_Throw<T>()
    {
        throw new UnreachableException();
#pragma warning disable CS0162 // Unreachable code detected
        yield return default; // has effect
#pragma warning restore CS0162 // Unreachable code detected
    }
}
