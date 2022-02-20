using JBSnorro.Algorithms;

namespace JBSnorro.Testing.IntertestDependency;

internal class TestIdentifier : ITestIdentifier
{
    public string FullName { get; init; } = default!;
    public bool IsType { get; init; }
    public string TypeName { get; }
    private readonly string? testName;
    public string TestName => IsType ? throw new InvalidTestConfigurationException() : testName!;

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
            this.TypeName =  FullName.SubstringUntilLast(".");
            this.testName = FullName.SubstringAfterLast(".");
        }
    }
    public static TestIdentifier FromString(string identifier, Type callerType)
    {
        bool isDefinitelyQualifiedIdentifier = identifier.Contains('.');
        if (isDefinitelyQualifiedIdentifier)
        {
            var type = ResolveType(identifier, callerType);
            if (type != null)
            {
                return From(type);
            }

            // if may be a fully qualified method
            string containingTypeName = identifier.SubstringUntilLast(".");
            var containingType = ResolveType(containingTypeName, callerType);
            if (containingType != null)
            {
                string methodName = identifier.SubstringAfterLast(".");
                var mi = callerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi != null)
                {
                    return From(mi);
                }
            }
        }
        else
        {
            // try to resolve the identifier as method first
            var mi = callerType.GetMethod(identifier, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
            {
                return From(mi);
            }

            var type = ResolveType(identifier, callerType);
            if (type != null)
            {
                return From(type);
            }
        }
        throw new InvalidTestConfigurationException($"The identifier '{identifier}' could not be located from type '{callerType.FullName}'");

        static Type? ResolveType(string typeName, Type callerType)
        {
            // first try finding type using .NET way, which requires full qualified
            var type = callerType.Assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            var assemblies = IIntertestDependencyTracker.TestAssemblies ?? new[] { callerType.Assembly };
            // the type name may not be fully qualified. Search some more
            return assemblies.FindType(typeName)
                             .Where(TestExtensions.IsTestClass)
                             .FirstOrDefault();
        }
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
