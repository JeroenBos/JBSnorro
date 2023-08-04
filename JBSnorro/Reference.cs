namespace JBSnorro;

public sealed class Reference<T> where T : notnull
{
	public T? Value { get; set; }

	public Reference(T? value)
	{
		Value = value;
	}
}
