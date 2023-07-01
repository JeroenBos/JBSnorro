#nullable enable
namespace JBSnorro.Collections;

class InsufficientBitsException : ArgumentOutOfRangeException
{
    public InsufficientBitsException() : base($"Insufficient bits remaining in stream") { }
    public InsufficientBitsException(string elementName) : base($"Insufficient bits remaining in stream to read '{elementName}'") { }
}