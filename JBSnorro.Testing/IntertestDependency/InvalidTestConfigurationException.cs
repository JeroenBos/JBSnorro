namespace JBSnorro.Testing.IntertestDependency;

/// <summary> Represents cases where the arguments to <see cref="IntertestExtensions.DependsOn"/> could not be resolved.</summary>
public class InvalidTestConfigurationException : Exception
{
    public InvalidTestConfigurationException() : base() { }
    public InvalidTestConfigurationException(string? message) : base(message) { }
    public InvalidTestConfigurationException(string? message, Exception innerException) : base(message, innerException) { }
}
