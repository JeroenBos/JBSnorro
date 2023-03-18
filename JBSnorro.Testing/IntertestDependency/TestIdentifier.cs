using JBSnorro.Algorithms;
using JBSnorro.Diagnostics;
using Microsoft.VisualBasic;

namespace JBSnorro.Testing.IntertestDependency;

internal class TestIdentifier : ITestIdentifier
{
    public string FullName { get; }
    public bool IsType { get; }
    public string TypeName { get; }
    private readonly string? testName;
    public string TestName => IsType ? throw new InvalidOperationException() : testName!;

    public TestIdentifier(string fullName, bool isType)
    {
        this.FullName = fullName;
        this.IsType = isType;
        if (isType)
        {
            this.TypeName = FullName;
        }
        else
        {
            this.TypeName = FullName.SubstringUntilLast(".");
            this.testName = FullName.SubstringAfterLast(".");
        }
    }
    /// <summary>
    /// Resolves an identifier. If fully qualified, resolves in the assemblies in <see cref="IIntertestDependencyTracker.TestAssemblies"/> or the assembly of <paramref name="callerType"/>.
    /// If not fully qualified, resolves it as a method in the specified type, or as a type in the assembly of <paramref name="callerType"/>.
    /// </summary>
    /// <param name="identifier">A method or type identifier, optionally fully qualified. </param>
    /// <param name="callerType">The type in which to resolve methods, or whose assembly to resolve types in. </param>
    public static TestIdentifier From(string identifier, Type callerType)
    {
        bool isQualified = identifier.Contains('.');
        if (isQualified)
        {
            return From(fullyQualifiedIdentifier: identifier, callerType.Assembly.ToSingleton());
        }

        // try to resolve the identifier as method first (before as type)
        var mi = callerType.GetMethod(identifier, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mi != null)
        {
            return From(mi);
        }

        var type = ResolveType(identifier, callerType.Assembly.ToSingleton());
        if (type != null)
        {
            return From(type);
        }
        throw new InvalidTestConfigurationException($"The identifier '{identifier}' could not be resolved from type '{callerType.FullName}'");
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fullyQualifiedIdentifier"></param>
    /// <param name="assembly">If null, <see cref="IIntertestDependencyTracker.TestAssemblies"/> must not be null. </param>
    /// <returns></returns>
    public static TestIdentifier From(string fullyQualifiedIdentifier, IEnumerable<Assembly>? assemblies = null)
    {
        Contract.Requires(!string.IsNullOrEmpty(fullyQualifiedIdentifier));
        assemblies ??= IIntertestDependencyTracker.TestAssemblies ?? throw new ContractException($"Either the argument '{nameof(assemblies)}' or '{nameof(IIntertestDependencyTracker)}.{nameof(IIntertestDependencyTracker.TestAssemblies)}' must be provided");

        var type = ResolveType(fullyQualifiedIdentifier, assemblies);
        if (type != null)
        {
            return From(type);
        }

        // if may be a fully qualified method
        string containingTypeName = fullyQualifiedIdentifier.SubstringUntilLast(".");
        var containingType = ResolveType(containingTypeName, assemblies);
        if (containingType != null)
        {
            string methodName = fullyQualifiedIdentifier.SubstringAfterLast(".");
            var mi = containingType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
            {
                return From(mi);
            }
        }
        throw new InvalidTestConfigurationException($"The identifier '{fullyQualifiedIdentifier}' could not be resolved"); 
    }
    public static TestIdentifier From(MethodInfo mi)
    {
        string fullName = mi.DeclaringType == null
                        ? mi.Name
                        : mi.DeclaringType.FullName + "." + mi.Name;
        return new TestIdentifier(fullName, isType: false);
    }
    public static TestIdentifier From(Type type)
    {
        string fullName = type.FullName!;
        return new TestIdentifier(fullName, isType: true);
    }
    public static TestIdentifier From(Type type, string methodName)
    {
        var mi = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        if (mi == null)
        {
            throw new InvalidTestConfigurationException($"No method with name '{methodName}' exists in type '{type.FullName}'");
        }
        return From(mi);
    }

    private static Type? ResolveType(string typeName, IEnumerable<Assembly> assemblies)
    {
        // first try finding type using .NET way, which requires fully qualified
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        // the type name may not be fully qualified. Search some more
        return assemblies.FindType(typeName)
                         .Where(TestExtensions.IsTestClass)
                         .FirstOrDefault();
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TestIdentifier);
    }
    public virtual bool Equals(ITestIdentifier? other)
    {
        return Equals(other as TestIdentifier);
    }
    public virtual bool Equals(TestIdentifier? other)
    {
        return other != null && other.FullName == this.FullName;
    }
    public override int GetHashCode()
    {
        return FullName.GetHashCode();
    }
    public override string ToString()
    {
        return FullName;
    }

    public static IEqualityComparer<TestIdentifier> TestIdentifierContainmentEqualityComparerInstance { get; } = new TestIdentifierContainmentEqualityComparer();
    class TestIdentifierContainmentEqualityComparer : IEqualityComparer<TestIdentifier>
    {
        public bool Equals(TestIdentifier? x, TestIdentifier? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;


            if (x.IsType)
            {
                return x.TypeName == y.TypeName;
            }
            if (y.IsType)
            {
                return x.TypeName == y.TypeName;
            }

            // neither is a type
            return x.FullName == y.FullName;
        }

        public int GetHashCode(TestIdentifier obj)
        {
            return obj.TypeName.GetHashCode();
        }
    }
}
