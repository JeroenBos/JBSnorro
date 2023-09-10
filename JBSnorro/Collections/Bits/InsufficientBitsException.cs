namespace JBSnorro.Collections.Bits;

public class InsufficientBitsException : ArgumentOutOfRangeException
{
    public InsufficientBitsException() : base($"Insufficient bits remaining in stream") { }
    public InsufficientBitsException(string elementName) : base($"Insufficient bits remaining in stream to read '{elementName}'") { }
}