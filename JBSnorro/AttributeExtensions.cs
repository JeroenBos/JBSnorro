using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using JBSnorro.Extensions.Internal;
using System.Diagnostics;
using System.Reflection;

namespace JBSnorro.Extensions
{
    public static class AttributeExtensions
    {
        public static bool UseCache { get; set; } = true;

        /// <summary>
        /// Recreates the attribute instance by invoking the constructor of the attribute data.
        /// </summary>
        public static Attribute Invoke(this CustomAttributeData data)
        {
            Contract.Requires(data != null);

            return (Attribute)data.Constructor.Invoke(data.ConstructorArguments.Select(arg => arg.Value).ToArray(data.ConstructorArguments.Count));
        }
        /// <summary>
        /// Recreates the attribute instance by invoking the constructor of the attribute data.
        /// </summary>
        public static TAttribute Invoke<TAttribute>(this CustomAttributeData data) where TAttribute : Attribute
        {
            Contract.Requires(data != null);
            Contract.Requires(data.AttributeType.IsAssignableFrom(typeof(TAttribute)));

            return (TAttribute)data.Invoke();
        }
        /// <summary>
        /// Gets the first attribute on the specified member based on the full name of the attribute type.
        /// </summary>
        public static Attribute GetCustomAttribute(this MemberInfo member, string fullAttributeTypeName)
        {
            Contract.Requires(member != null);
            Contract.Requires(!string.IsNullOrEmpty(fullAttributeTypeName));

            return member.GetCustomAttributesData()
                       .Where(attribute => attribute.AttributeType.FullName == fullAttributeTypeName)
                       .FirstOrDefault()
                       ?.Invoke();
        }

        /// <summary>
        /// Gets the first attribute on the specified method that has a full name in the specified list; or null if none match.
        /// </summary>
        /// <param paramname="inherit"> Indicates whether attributes inheriting from any attribute with a specified full name satisfy as well. </param>
        [DebuggerHidden]
        public static Attribute GetAttribute(this MemberInfo member, IReadOnlyCollection<string> attributeFullNames, bool inherit = true)
        {
            return GetAttribute(member, attributeFullNames, out var _, inherit);
        }
#nullable enable
        /// <summary>
        /// Gets the first attribute on the specified method that has a full name in the specified list; or null if none match.
        /// </summary>
        [DebuggerHidden]
        public static Attribute? GetAttribute(this MemberInfo member, IReadOnlyCollection<string> attributeFullNames, out string? fullName, bool inherit = true)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (attributeFullNames == null) throw new ArgumentNullException(nameof(attributeFullNames));

            // We use a custom attribute cache, because this call appears to be a performance bottleneck 
            // Note that GetCustomAttributes<Attribute>() does not return the same as GetCustomAttributes() for e.g. inherited attributes
            foreach (var attribute in UseCache ? CustomAttributeCache<Attribute>.Instance.GetCustomAttributes(member) : member.GetCustomAttributes<Attribute>())
            {
                Type attributeType = attribute.GetType();
                foreach (var attributeBaseType in inherit ? attributeType.GetBaseTypesAndSelf() : new[] { attributeType })
                {
                    if (attributeFullNames.Contains(attributeBaseType.FullName))
                    {
                        fullName = attributeBaseType.FullName;
                        return attribute;
                    }
                }
            }
            fullName = null;
            return null;
        }
#nullable restore
        /// <summary>
        /// Gets whether the specified info has an attribute with the specified full name.
        /// </summary>
        public static bool HasAttribute<TAttribute>(this MemberInfo member, bool inherit = true) where TAttribute : Attribute
        {
            var result = HasAttribute(member, new[] { typeof(TAttribute).FullName }, inherit);
            return result;
        }
        /// <summary>
        /// Gets whether the specified info has an attribute with the specified full name.
        /// </summary>
        [DebuggerHidden]
        public static bool HasAttribute(this MemberInfo member, params string[] attributeFullNames)
        {
            return HasAttribute(member, (IReadOnlyCollection<string>)attributeFullNames);
        }
        /// <summary>
        /// Gets whether the specified info has an attribute with the specified full name.
        /// </summary>
        [DebuggerHidden]
        public static bool HasAttribute(this MemberInfo member, IReadOnlyCollection<string> attributeFullNames, bool inherit = true)
        {
            return GetAttribute(member, attributeFullNames, inherit) != null;
        }
        /// <summary>
        /// Gets a function that computer whether the specified member info has an attribute with the specified full name.
        /// </summary>
        [DebuggerHidden]
        public static Func<TMemberInfo, bool> HasAttributeDelegate<TMemberInfo>(params string[] attributeFullNames) where TMemberInfo : MemberInfo
        {
            // This is not a extension method taking a lambda because you can't apply [DebuggerHidden] to a lambda
            Contract.Requires(attributeFullNames != null);
            Contract.Requires(attributeFullNames.Length != 0);

            return [DebuggerHidden] (TMemberInfo member) =>
            {
                return member.HasAttribute(attributeFullNames);
            };
        }

        /// <summary>
        /// Gets a function that computer whether the specified member info has an attribute with the specified full name.
        /// </summary>
        [DebuggerHidden]
        public static Func<MemberInfo, bool> HasAttributeDelegate(params string[] attributeFullNames) => HasAttributeDelegate<MemberInfo>(attributeFullNames);
    }
}
