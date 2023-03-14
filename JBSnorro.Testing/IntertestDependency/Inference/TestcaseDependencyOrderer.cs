using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;
using JBSnorro;
using JBSnorro.Extensions;
using JBSnorro.Graphs;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using System.Diagnostics;

namespace JBSnorro.Testing.IntertestDependency.Inference;

public class TestcaseDependencyOrderer : ITestCaseOrderer
{
    public static TestcaseDependencyOrderer Instance = new();

    private TestcaseDependencyOrderer() { }


    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        var totalOrderer = new TotalOrderer<ITestCase>();
        foreach (TTestCase testCase in testCases)
        {
            foreach (IEnumerable<ITestCase> dependencies in testCase.TestMethod.Method.GetCustomAttributes(typeof(DependsOnAttribute)).Select(getTestCases))
            {
                totalOrderer.Add(testCase, dependencies);
            }
        }

        return totalOrderer.GetTotalOrder().Select(testCase => (TTestCase)testCase);

        IEnumerable<ITestCase> getTestCases(IAttributeInfo dependsOnAttributeInfo)
        {
            TestCaseFromAttribute.Create(// TODO: infer what test(s) the attribute is pointing to. Hopefully through reuse of what I've built previously

        }
    }

    class TestCaseFromAttribute : LongLivedMarshalByRefObject, ITestCase
    {
        public required ITestMethod TestMethod { get; init; }
        public object?[]? TestMethodArguments { get; init; }

        public static TestCaseFromAttribute Create(string typeName, string testMethodName)
        {
            ITypeInfo t = new TypeInfoFromAttribute()
            {
                Name = typeName,
            };
            IMethodInfo a = new MethodInfoFromAttribute()
            {
                Name = testMethodName,
                Type = t
            };
            ITestClass c = new TestClassFromAttribute()
            {
                Class = t,
            };
            ITestMethod m = new TestMethodFromAttribute()
            {
                Method = a,
                TestClass = c,
            };
            return new TestCaseFromAttribute()
            {
                TestMethod = m
            };
        }


        string ITestCase.DisplayName => throw new NotImplementedException();
        string ITestCase.SkipReason => throw new NotImplementedException();
        ISourceInformation ITestCase.SourceInformation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Dictionary<string, List<string>> ITestCase.Traits => throw new NotImplementedException();
        string ITestCase.UniqueID => throw new NotImplementedException();
        void IXunitSerializable.Deserialize(IXunitSerializationInfo info)
        {
            throw new NotImplementedException();
        }
        void IXunitSerializable.Serialize(IXunitSerializationInfo info)
        {
            throw new NotImplementedException();
        }
    }
    class ITestCaseEqualityComparer : IEqualityComparer<ITestCase>, IEqualityComparer<ITestMethod>, IEqualityComparer<IMethodInfo>, IEqualityComparer<ITestClass>, IEqualityComparer<ITypeInfo>
    {
        public bool Equals(ITestCase? x, ITestCase? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;

            return this.Equals(x.TestMethod, y.TestMethod)
                && SequenceEqualityComparer<object?>.AnyOrderComparer.Equals(x.TestMethodArguments, y.TestMethodArguments);
        }
        public int GetHashCode(ITestCase obj)
        {
            return this.GetHashCode(obj.TestMethod) + (obj.TestMethodArguments?.GetHashCode() ?? 0);
        }


        public bool Equals(ITestMethod? x, ITestMethod? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;

            return this.Equals(x.TestClass, y.TestClass) 
                && this.Equals(x.Method, y.Method);
        }
        public int GetHashCode(ITestMethod obj)
        {
            return this.GetHashCode(obj.TestClass) + this.GetHashCode(obj.Method);
        }


        public bool Equals(ITestClass? x, ITestClass? y)
        {
            if (x is null) return y is null;
            if (y is null) return false;

            return this.Equals(x.Class, y.Class);
        }
        public int GetHashCode(ITestClass obj)
        {
            return this.GetHashCode(obj.Class);
        }

        public bool Equals(ITypeInfo? x, ITypeInfo? y)
        {
            return x?.Name == y?.Name;
        }
        public int GetHashCode([DisallowNull] ITypeInfo obj)
        {
            return obj.Name.GetHashCode();
        }


        public bool Equals(IMethodInfo? x, IMethodInfo? y)
        {
            return x?.Name == y?.Name;
        }
        public int GetHashCode(IMethodInfo obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    internal class TestMethodFromAttribute : ITestMethod
    {
        public required IMethodInfo Method { get; init; }
        public required ITestClass TestClass { get; init; }

        void IXunitSerializable.Deserialize(IXunitSerializationInfo info) => throw new NotImplementedException();
        void IXunitSerializable.Serialize(IXunitSerializationInfo info) => throw new NotImplementedException();
    }
    internal class MethodInfoFromAttribute : Xunit.Abstractions.IMethodInfo
    {
        public string? Name { get; init; }
        public ITypeInfo? Type { get; init; }



        bool IMethodInfo.IsAbstract => throw new NotImplementedException();
        bool IMethodInfo.IsGenericMethodDefinition => throw new NotImplementedException();
        bool IMethodInfo.IsPublic => throw new NotImplementedException();
        bool IMethodInfo.IsStatic => throw new NotImplementedException();
        ITypeInfo? IMethodInfo.ReturnType => throw new NotImplementedException();
        IEnumerable<ITypeInfo> IMethodInfo.GetGenericArguments() => throw new NotImplementedException();
        IEnumerable<IParameterInfo> IMethodInfo.GetParameters() => throw new NotImplementedException();
        IMethodInfo IMethodInfo.MakeGenericMethod(params ITypeInfo[] typeArguments) => throw new NotImplementedException();
        IEnumerable<IAttributeInfo> IMethodInfo.GetCustomAttributes(string assemblyQualifiedAttributeTypeName) => throw new NotImplementedException();
    }
    internal class TestClassFromAttribute : ITestClass
    {
        public required ITypeInfo Class { get; init; }


        ITestCollection ITestClass.TestCollection => throw new NotImplementedException();
        void IXunitSerializable.Deserialize(IXunitSerializationInfo info) => throw new NotImplementedException();
        void IXunitSerializable.Serialize(IXunitSerializationInfo info) => throw new NotImplementedException();
    }
    internal class TypeInfoFromAttribute : ITypeInfo
    {
        /// <summary>
        /// Fully qualified.
        /// </summary>
        public required string Name { get; init; }

        IAssemblyInfo ITypeInfo.Assembly => throw new NotImplementedException();
        ITypeInfo ITypeInfo.BaseType => throw new NotImplementedException();
        IEnumerable<ITypeInfo> ITypeInfo.Interfaces => throw new NotImplementedException();
        bool ITypeInfo.IsAbstract => throw new NotImplementedException();
        bool ITypeInfo.IsGenericParameter => throw new NotImplementedException();
        bool ITypeInfo.IsGenericType => throw new NotImplementedException();
        bool ITypeInfo.IsSealed => throw new NotImplementedException();
        bool ITypeInfo.IsValueType => throw new NotImplementedException();
        IEnumerable<IAttributeInfo> ITypeInfo.GetCustomAttributes(string assemblyQualifiedAttributeTypeName) => throw new NotImplementedException();
        IEnumerable<ITypeInfo> ITypeInfo.GetGenericArguments() => throw new NotImplementedException();
        IMethodInfo ITypeInfo.GetMethod(string methodName, bool includePrivateMethod) => throw new NotImplementedException();
        IEnumerable<IMethodInfo> ITypeInfo.GetMethods(bool includePrivateMethods) => throw new NotImplementedException();
    }
}