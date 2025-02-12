namespace JBSnorro;

public sealed class Either<T, U> where T : notnull where U : notnull
{
    public T? Value1 { get; }
    public U? Value2 { get; }
    public Either(T value1)
    {
        this.Value1 = value1 ?? throw new ArgumentNullException(nameof(value1));
    }
    public Either(U value2)
    {
        this.Value2 = value2 ?? throw new ArgumentNullException(nameof(value2));
    }

    public bool TryGet(out T value1)
    {
        value1 = this.Value1!;
        return value1 is not null;
    }
    public bool TryGet(out U value2)
    {
        value2 = this.Value2!;
        return value2 is not null;
    }
    /// <summary>
    /// Extracts the value, whichever one it is.
    /// </summary>
    /// <returns><see langword="true"/> if this is a <typeparamref name="T"/>, <see langword="false"/> if this is a <typeparamref name="U"/>. </returns>
    public bool Get(out T value1, out U value2)
    {
        value1 = this.Value1!;
        value2 = this.Value2!;
        return value1 is not null;
    }

    public static implicit operator Either<T, U>(T value) => new(value);
    public static implicit operator Either<T, U>(U value) => new(value);
}
