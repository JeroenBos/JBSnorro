namespace JBSnorro;

public sealed class Reference<T>
{
	public T Value { get; set; }

	public Reference(T value)
	{
		Value = value;
	}
}
