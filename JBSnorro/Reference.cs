namespace JBSnorro;

public sealed class Reference<T> where T : notnull
{
	private static int debug;
	private T? _value;
	public T? Value
	{
		get => _value;
		set
		{
			if (typeof(T) == typeof(long) && (long)(object)value == 0)
			{
				Console.WriteLine("Writing zero");
			}
			this._value = value;
			debug++;
		}
	}

	public Reference()
	{
	}
	public Reference(T? value)
	{
		Value = value;
	}
}
