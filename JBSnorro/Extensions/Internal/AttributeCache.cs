using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace JBSnorro.Extensions.Internal;

internal class CustomAttributeCache<TAttribute> where TAttribute : Attribute
{
    public static CustomAttributeCache<TAttribute> Instance { get; } = new();

    private ImmutableDictionary<MemberInfo, ReadOnlyCollection<TAttribute>> cache = ImmutableDictionary<MemberInfo, ReadOnlyCollection<TAttribute>>.Empty;
    [DebuggerHidden]
    public ReadOnlyCollection<TAttribute> GetCustomAttributes(MemberInfo key)
    {
        return ImmutableInterlocked.GetOrAdd(ref cache, key, compute);
    }
    [DebuggerHidden]
    private static ReadOnlyCollection<TAttribute> compute(MemberInfo key)
    {
        return key.GetCustomAttributes<TAttribute>().ToReadOnlyList();
    }
}
