namespace JBSnorro.Testing.IntertestDependency;

public class InvalidTestConfigurationException : Exception
{
    public InvalidTestConfigurationException() : base() { }
    public InvalidTestConfigurationException(string? message) : base(message) { }
    public InvalidTestConfigurationException(string? message, Exception innerException) : base(message, innerException) { }
}
