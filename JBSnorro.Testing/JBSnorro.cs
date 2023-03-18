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
}
