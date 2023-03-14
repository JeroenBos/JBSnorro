namespace JBSnorro.Testing.IntertestDependency.Inference;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
sealed class DependsOnAttribute : Attribute
{
    public string Method { get; set; }
    public DependsOnAttribute(string method)
    {
        this.Method = method;
    }
}
