using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class Test
{
    public Type Type { get; }
    public MethodInfo Method { get; }
    [DebuggerHidden]
    public Test(Type type, MethodInfo method)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if (method == null) throw new ArgumentNullException(nameof(method));
        (this.Type, this.Method) = (type, method);
    }
    public void Deconstruct(out Type Type, out MethodInfo Method)
    {
        Type = this.Type;
        Method = this.Method;
    }
    public static implicit operator Test((Type, MethodInfo) t) => new Test(t.Item1, t.Item2);

    [DebuggerHidden]
    public static MethodInfo GetMethod(Test test) => test.Method;
    [DebuggerHidden]
    public static Type GetType(Test test) => test.Type;
}

namespace JBSnorro.Testing
{
    public static class TestExtensions
    {
        /// <summary>
        /// Gets all public types in the specified assembly that are annotated with `Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute`.
        /// </summary>
        [DebuggerHidden]
        public static IEnumerable<Type> GetTestTypes(this Assembly assembly)
        {
            Contract.Requires(assembly != null);

            var msTestClasses = assembly.GetTypes().Where(AttributeExtensions.HasAttributeDelegate<Type>("Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute"));
            var xUnitTestClasses = assembly.GetTypes().Where(isxUnitTestClass).ToList();
            return msTestClasses.Concat(xUnitTestClasses).Where(isNotAbstract).Where(isNotStatic);

            [DebuggerHidden] static bool isNotStatic(Type type) => !type.IsStatic();
            [DebuggerHidden] static bool isNotAbstract(MemberInfo member) => !member.IsAbstract();
        }
        [DebuggerHidden]
        private static bool isxUnitTestClass(Type type)
        {
            // This function is non-local to enable applying the DebuggerHidden attribute

            // All types could potentially host xUnit's facts, but we require there's at least one method marked xUnit.Fact in there
            return type.GetMethods().Any(AttributeExtensions.HasAttributeDelegate("Xunit.FactAttribute", "Xunit.Sdk.XunitTest"));
        }
        [DebuggerHidden]
        public static IEnumerable<Test> GetTestMethods(this Assembly assembly, string predicate)
        {
            return GetTestMethods(assembly).Where([DebuggerHidden] (test) => matches(test, predicate));
        }

        [DebuggerHidden]
        public static IEnumerable<Test> GetTestMethods(this Assembly assembly)
        {
            return assembly.GetTestTypes()
                           .SelectMany(GetTestMethods);
        }
        /// <summary>
        /// Extracted to be able to apply DebuggerHidden on it.
        /// </summary>
        [DebuggerHidden]
        public static IEnumerable<Test> GetTestMethods(Type type)
        {
            string[] names = new[] {
                "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute",
                "Xunit.FactAttribute",
            };

            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                       .Where(AttributeExtensions.HasAttributeDelegate<MethodInfo>(names))
                       .Where(isNotAbstract)
                       .Select(toTest);

            [DebuggerHidden]
            Test toTest(MethodInfo mi) => new Test(type, mi);
            [DebuggerHidden]
            static bool isNotAbstract(MemberInfo member) => !member.IsAbstract();
        }

        [DebuggerHidden]
        private static IEnumerable<MethodInfo> GetInitializationMethod(Type type)
        {
            string[] names = new[] {
                "Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute",
				// in xUnit it's the parameterless constructor
			};

            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                       .Where(AttributeExtensions.HasAttributeDelegate<MethodInfo>("Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute"));
        }
        [DebuggerHidden]
        private static IEnumerable<MethodInfo> GetCleanupMethod(Type type)
        {
            string[] names = new[] {
                "Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute",
            };
            var msTestCleanupMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                           .Where(AttributeExtensions.HasAttributeDelegate<MethodInfo>(names));

            var xUnitTestCleanupMethod = type.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (type.Implements(typeof(IDisposable)))
                // IDisposable is already honoured in cleanup
                // This also enables pattern-based on top of than interface based
                xUnitTestCleanupMethod = null;

            return msTestCleanupMethods.ConcatIfNotNull(xUnitTestCleanupMethod);
        }
        [DebuggerHidden]
        public static IEnumerable<Func<Task>> GetExecutableTestMethods(this Assembly assembly, string predicate)
        {
            return GetTestMethods(assembly, predicate).Select(toExecutable);
        }
        /// <returns> Even if the test is void-returning, it is still wrapped in a Task.</returns>
        public static IEnumerable<Func<Task>> GetExecutableTestMethods(this Assembly assembly)
        {
            return GetTestMethods(assembly).Select(toExecutable);
        }
        [DebuggerHidden]
        private static bool matches(Test test, string predicate)
        {
            const string sep = "::";
            // The pattern matches against 

            // TYPE_FULL_NAME::TESTNAME
            // where both identifiers may contain the * wildcard
            // and the default of both identifiers is * (i.e. when omitted). That means that these are all equivalent:
            // ``', `::`, `*`, `*::`, `::*`

            string typePattern = "*";
            string methodPattern = "*";
            int separatorIndex = predicate.IndexOf(sep);
            if (separatorIndex != -1)
            {
                if (separatorIndex != 0)
                {
                    typePattern = predicate[..separatorIndex].Trim();
                }
                if (separatorIndex != predicate.Length - sep.Length)
                {
                    methodPattern = predicate[(separatorIndex + sep.Length)..].Trim();
                }

                return match(methodPattern, test.Method.Name) && match(typePattern, test.Type.FullName);
            }
            else
            {
                return match(predicate, test.Method.Name) || match(predicate, test.Type.FullName);
            }

            static bool match(string pattern, string candidate)
            {
                var regexString = Regex.Escape(pattern.ToLowerInvariant()).Replace("\\*", ".*").Replace("\\?", ".");
                var regex = new Regex(regexString);
                return regex.IsMatch(candidate.ToLowerInvariant());
            }
        }
        [DebuggerHidden]
        private static Func<Task> toExecutable(Test test)
        {
            CheckTestInvariants(test);

            var ctor = test.Type.GetConstructor(Type.EmptyTypes);
            var initAndCleanup = test.Type.GetInitAndCleanupMethods();
            [DebuggerHidden] Task curry() => execute(test.Method, ctor, initAndCleanup);
            return curry;
            // return DebuggerHidden.Curry<MethodInfo, ConstructorInfo, (IReadOnlyList<MethodInfo>, IReadOnlyList<MethodInfo>), Task>(execute, test.Method, ctor, initAndCleanup);
        }
        /// <summary>
        /// A helper method which asserts that the signature of the specified method could be a test method.
        /// </summary>
        [DebuggerHidden]
        public static void CheckTestInvariants(Test test)
        {
            (Type type, MethodInfo method) = test;
            if (type.IsAbstract)
                throw new InvalidOperationException($"The specified method is in an abstract class ({type.FullName})");
            if (type.IsStatic())
                throw new InvalidOperationException($"The specified method is in a static class ({type.FullName})");

            if (method.GetParameters().Length != 0)
                throw new InvalidOperationException("The specified method accepts a nonzero number of parameters");
            if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                throw new InvalidOperationException("The specified method is generic");
            if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
                throw new InvalidOperationException("The specified method does not return void or Task");
            if (method.IsStatic)
                throw new InvalidOperationException($"The specified method is static ({type.FullName}.{method.Name})");
            if (method.IsAbstract)
                throw new InvalidOperationException($"The specified method is abstract ({type.FullName}.{method.Name})");

            var ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidOperationException($"The type '{type}' does not have a parameterless constructor");
        }

        [DebuggerHidden]
        private static (IReadOnlyList<MethodInfo> Inits, IReadOnlyList<MethodInfo> Cleanups) GetInitAndCleanupMethods(this Type testType)
        {
            var initializationMethods = GetInitializationMethod(testType).ToReadOnlyList();
            var cleanupMethods = GetCleanupMethod(testType).ToReadOnlyList();
            return (initializationMethods, cleanupMethods);
        }

#nullable enable

        [DebuggerHidden]
        private static async Task execute(MethodInfo method, ConstructorInfo ctor, (IReadOnlyList<MethodInfo> Inits, IReadOnlyList<MethodInfo> Cleanups) wrappers)
        {
            object? testClass = null;
            try
            {
                try
                {
                    testClass = ctor.Invoke(EmptyCollection<object>.Array);
                }
                catch (TargetInvocationException)
                {
                    testClass = reinvoke();
                    object reinvoke() // eschews debuggerhidden attribute so that I can debug from here
                    {
                        // the following line will throw:
                        return ctor.Invoke(EmptyCollection<object>.Array);
                    }
                }
                // Console.WriteLine($"Executing {ctor.DeclaringType!.Name}:{method.Name}");
                foreach (var mi in wrappers.Inits.Concat(method))
                {
                    var returnValue = mi.ToDelegate(testClass)();
                    if (returnValue is Task task)
                    {
                        await task;
                    }
                }
            }
            finally
            {
                try
                {
                    foreach (var cleaner in wrappers.Cleanups)
                    {
                        var returnValue = cleaner.ToDelegate(testClass)();
                        if (returnValue is Task task)
                        {
                            await task;
                        }
                    }
                }
                finally
                {
                    if (testClass is IAsyncDisposable asyncDisposableTestClass)
                        await asyncDisposableTestClass.DisposeAsync();
                    if (testClass is IDisposable disposableTestClass)
                        disposableTestClass.Dispose();
                }
            }
        }

        /// <summary>
        /// Known limitations: 
        /// - calling one-time (class) setup/tear down calls isn't implemented
        /// - NUnit not implemented
        /// A special argument "--args" can be supplied to ignore all previous arguments.
        /// </summary>
        public static async Task DefaultMainTestProjectImplementation(string[] args, Assembly? assemblyUnderTest = null)
        {
            var originalArgs = args;
            args = args[(args.IndexOf("--args") + 1)..];
            if (args.Length > 1)
            {
                foreach (var arg in args)
                    Console.WriteLine(arg);
                throwArgumentException("Expected one argument; got " + args.Length);
            }
            if (args.Length != 1)
            {
                throwArgumentException("Expected a regex expression to select tests to execute");
            }
            string filter = args[0];
            var assembly = assemblyUnderTest ?? Assembly.GetEntryAssembly();
            var tests = assembly.GetExecutableTestMethods(filter).ToList();
            Console.WriteLine($"Executing {tests.Count} tests" + (tests.Count == 0 ? $" matching '{filter}'" : ""));
            foreach (var test in tests)
            {
                await test();
            }
            Console.WriteLine("Done");
            Thread.Sleep(1000);

            // is local function to eschew [DebuggerHidden]
            void throwArgumentException(string message)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
