using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
    /// <summary> Allows for obtaining the properties of a PropertyMutatedEventArgs&lt;T&gt; without knowing T compile-time. </summary>
    public interface IPropertyMutatedEventArgs
    {
        object? OldValue { get; }
        object? NewValue { get; }

        string PropertyName { get; }
    }
    /// <summary> A property changed event argument that also holds the old and new values of the mutated property. </summary>
    /// <see href="http://stackoverflow.com/questions/7677854/notifypropertychanged-event-where-event-args-contain-the-old-value"/>
    public class PropertyMutatedEventArgs<T> : PropertyChangedEventArgs, IPropertyMutatedEventArgs
    {
        public virtual T OldValue { get; }
        public virtual T NewValue { get; }

        object? IPropertyMutatedEventArgs.OldValue => OldValue;
        object? IPropertyMutatedEventArgs.NewValue => NewValue;

        [DebuggerHidden]
        public PropertyMutatedEventArgs(string propertyName, T oldValue, T newValue)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
        public override string PropertyName
        {
            get => base.PropertyName!;
        }
    }

    public static class PropertyMutatedEventArgsExtensions
    {
        /// <summary> Creates an instance of <see cref="PropertyMutatedEventArgs{T}"/> without knowing T at compile time. </summary>
        public static PropertyChangedEventArgs Create(string propertyName, Type genericParameterType, object? oldValue, object? newValue)
        {
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
            Contract.Requires(genericParameterType != null);
            Contract.Requires(ReferenceEquals(oldValue, null) || genericParameterType.IsInstanceOfType(oldValue));
            Contract.Requires(ReferenceEquals(newValue, null) || genericParameterType.IsInstanceOfType(newValue));

            oldValue = oldValue ?? genericParameterType.GetDefault();
            newValue = newValue ?? genericParameterType.GetDefault();

            var result = typeof(PropertyMutatedEventArgs<>).MakeGenericType(genericParameterType)
                                                           .GetConstructor(new Type[] { typeof(string), genericParameterType, genericParameterType })!
                                                           .Invoke(new[] { propertyName, oldValue, newValue });

            return (PropertyChangedEventArgs)result;
        }
        /// <summary>
        /// Creates a <see cref="PropertyMutatedEventArgs{T}"/> for the specified property on the specified object.
        /// </summary>
        /// <param name="source"> The object contained the named property. </param>
        /// <param name="propertyName"> The name of the property to create a <see cref="PropertyMutatedEventArgs{T}"/> for. </param>
        /// <param name="sourceType"> The type of <paramref name="source"/> or one of the interfaces it implements from which the property name is to be fetched. 
        /// Null indicates that the property is not an explicitly implemented property. </param>
        public static PropertyChangedEventArgs From(object source, string propertyName, Type? sourceType = null)
        {
            return From(source, propertyName, oldValue: null, sourceType);
        }
        /// <summary>
        /// Creates a <see cref="PropertyMutatedEventArgs{T}"/> for the specified property on the specified object.
        /// </summary>
        /// <param name="source"> The object contained the named property. </param>
        /// <param name="propertyName"> The name of the property to create a <see cref="PropertyMutatedEventArgs{T}"/> for. </param>
        /// <param name="oldValue"> The previous value of the property. This cannot be retrieved any more, so must be specified. 
        /// For non-reference types, null is converted the the default of the property type. </param>
        /// <param name="sourceType"> The type of <paramref name="source"/> or one of the interfaces it implements from which the property name is to be fetched. 
        /// Null indicates that the property is not an explicitly implemented property. </param>
        public static PropertyChangedEventArgs From(object source, string propertyName, object? oldValue, Type? sourceType = null)
        {
            Contract.Requires(source != null);
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
            Contract.Requires(sourceType == null || sourceType.IsAssignableFrom(source.GetType()));

            object newValue = EventBindingExtensions.GetPropertyValue(source, propertyName, sourceType ?? source.GetType());
            return From(source, propertyName, oldValue: oldValue, newValue: newValue, sourceType);
        }
        /// <summary>
        /// Creates a <see cref="PropertyMutatedEventArgs{T}"/> for the specified property on the specified object.
        /// </summary>
        /// <param name="source"> The object contained the named property. </param>
        /// <param name="propertyName"> The name of the property to create a <see cref="PropertyMutatedEventArgs{T}"/> for. </param>
        /// <param name="oldValue"> The previous value of the property. This cannot be retrieved any more, so must be specified. 
        /// For non-reference types, null is converted the the default of the property type. </param>
        /// <param name="newValue"> The new value of the property on the source. </param>
        /// <param name="sourceType"> The type of <paramref name="source"/> or one of the interfaces it implements from which the property name is to be fetched. 
        /// Null indicates that the property is not an explicitly implemented property. </param>
        public static PropertyChangedEventArgs From(object source, string propertyName, object? oldValue, object? newValue, Type? sourceType = null)
        {
            sourceType = sourceType ?? source.GetType();
            Type parameterType = sourceType.GetPropertyType(propertyName);

            return Create(propertyName, parameterType, oldValue, newValue);
        }
        /// <summary>
        /// Returns the specified args as <see cref="PropertyMutatedEventArgs{T}"/>.
        /// </summary>
        public static PropertyMutatedEventArgs<T> OfType<T>(this IPropertyMutatedEventArgs args)
        {
            Contract.Requires(args != null);

            return (args as PropertyMutatedEventArgs<T>)
                ?? new PropertyMutatedEventArgs<T>(args.PropertyName, (T)args.OldValue!, (T)args.NewValue!);
        }
    }
    /// <summary>
    /// Like the <see cref="PropertyChangedEventHandler"/>, but where the old and new value of the property are specified.
    /// </summary>
    public delegate void PropertyMutatedEventHandler(object sender, IPropertyMutatedEventArgs e);
    /// <summary>
    /// Like the <see cref="PropertyChangedEventHandler"/>, but where the old and new value of the property are specified with type information.
    /// </summary>
    public delegate void PropertyMutatedEventHandler<T>(object sender, PropertyMutatedEventArgs<T> e);
}
