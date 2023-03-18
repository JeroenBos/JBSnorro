using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static JBSnorro.Global;

namespace JBSnorro.Extensions
{
	/// <summary> This class contains extension methods on the type <see cref="System.Type"/>. </summary>
	public static class TypeExtensions
	{
		/// <summary> Gets the (boxed) default instance of the specified type. </summary>
		public static object GetDefault(this Type type)
		{
			if (type.IsValueType)
			{
				return Activator.CreateInstance(type);
			}
			return null;
		}

		/// <summary> Gets all public fields on the specified type of the type specified as generic type argument. </summary>
		/// <param name="type"> The type whose fields to get. </param>
		/// <typeparam name="TField"> The type of the fields to get. </typeparam>
		public static IEnumerable<FieldInfo> GetFieldsOfType<TField>(this Type type)
		{
			Contract.Requires(type != null);

			return type.GetFieldsOfType<TField>(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}
		/// <summary> Gets all fields on the specified type of the type specified as generic type argument. </summary>
		/// <param name="flags"> The flags indicating how the fields are searched. </param>
		/// <param name="type"> The type whose fields to get. </param>
		/// <typeparam name="TField"> The type of the fields to get. </typeparam>
		public static IEnumerable<FieldInfo> GetFieldsOfType<TField>(this Type type, BindingFlags flags)
		{
			Contract.Requires(type != null);

			return type.GetFields(flags)
					   .Where(fieldInfo => fieldInfo.FieldType == typeof(TField));
		}

		/// <summary> Returns whether the specified object has any of the specified types exactly. </summary>
		public static bool HasAnyTypeOf(this object obj, params Type[] types)
		{
			Contract.Requires(obj != null);

			return obj.GetType().IsAnyOf(types);
		}
		/// <summary> Gets whether the specified type can be assigned to any of the specified candidates. </summary>
		public static bool IsAnyOf(this Type item, params Type[] candidates)
		{
			return item.IsAnyOf(InterfaceWraps.ToEqualityComparer<Type>((a, b) => b.IsAssignableFrom(a)), candidates);
		}

		/// <summary> Gets whether the specified method has the same signature as the specified generic type argument. </summary>
		/// <typeparam name="F"> The delegate type of which is compared to the method signature for equality. <typeparamref name="F"/> stands for function. </typeparam>
		public static bool HasSignature<F>(this MethodInfo method)
		{
			Contract.Requires(method != null);

			return HasSignature(method, typeof(F));
		}
		/// <summary> Gets whether the specified method has a signature equal to the specified type argument. </summary>
		public static bool HasSignature(this MethodInfo method, Type delegateType)
		{
			Contract.Requires(method != null);
			Contract.Requires(delegateType != null);
			Contract.Requires(delegateType.IsSubclassOf(typeof(MulticastDelegate)));

			MethodInfo f = delegateType.GetMethod("Invoke");

			if (f.ReturnType != method.ReturnType)
			{
				return false;
			}

			// TODO: compare other parameter properties
			var result = EnumerableExtensions.SequenceEqualBy(method.GetParameters(), f.GetParameters(), parameter => parameter.ParameterType, ReferenceEqualityComparer.Instance);
			return result;
		}
		/// <summary> Gets whether the specified event has the a handler signature equal to the specified generic type argument. </summary>
		/// <typeparam name="F"> The delegate type which is compared to the event handler signature for equality. <typeparamref name="F"/> stands for function. </typeparam>
		public static bool HasSignature<F>(this EventInfo eventInfo)
		{
			Contract.Requires(eventInfo != null);

			MethodInfo f = typeof(F).GetMethod("Invoke");

			return f.HasSignature(eventInfo.EventHandlerType);
		}
		/// <summary> Gets whether the specified event has the a handler signature of the form Action&lt;object, TEventArgs&gt;. </summary>
		/// <typeparam name="TEventArgs"> The event arg type used for comparison. </typeparam> 
		public static bool HasHandlerSignature<TEventArgs>(this EventInfo eventInfo) where TEventArgs : EventArgs
		{
			Contract.Requires(eventInfo != null);

			return eventInfo.HasSignature<Action<object, TEventArgs>>();
		}
		/// <summary> Yields all specified methods that have the signature of the generic type argument. </summary>
		/// <typeparam name="F"> The delegate type which a method info must match to be yielded. <typeparamref name="F"/> stands for function. </typeparam>
		public static IEnumerable<MethodInfo> OfSignature<F>(this IEnumerable<MethodInfo> methods)
		{
			EnsureSingleEnumerationDEBUG(ref methods);

			Contract.Requires(methods != null);
			Contract.RequiresForAll(methods, NotNull);
			Contract.Requires(typeof(F).IsSubclassOf(typeof(MulticastDelegate)));

			return methods.Where(HasSignature<F>);
		}
		/// <summary> Gets delegates representing the specified methods with a signature <typeparamref name="F"/> invoked on the specified target. </summary>
		/// <param name="target"> The object to invoke the methods on. Specify null when the methods are static. </param>
		/// <typeparam name="F"> The delegate type which a method info must match to be yielded. <typeparamref name="F"/> stands for function. </typeparam>
		public static IEnumerable<F> OfSignature<F>(this IEnumerable<MethodInfo> methods, object target)
		{
			Contract.Requires(methods != null);
			Contract.Requires(typeof(F).IsSubclassOf(typeof(MulticastDelegate)));

			return methods.OfSignature<F>()
						  .Select(method => ToSignature<F>(method, target));
		}

		/// <summary> Creates a delegate of generic type argument type for the specified method invoked on the specified target. </summary>
		/// <typeparam name="F"> The delegate type to which the specified method is converted. <typeparamref name="F"/> stands for function. </typeparam>
		public static F ToSignature<F>(this MethodInfo method, object target = null)
		{
			Contract.Requires(method != null);
			Contract.Requires(typeof(F).IsSubclassOf(typeof(MulticastDelegate)));

			return (F)(object)Delegate.CreateDelegate(typeof(F), target, method, throwOnBindFailure: true);
		}

		/// <summary>
		/// Contains the requirements for the ToAction and ToFunc methods below.
		/// </summary>
		private static void RequiresConveribleToSignature<F>(this MethodInfo method, int receivedTypeParameterCount, out bool shortCircuitToSignature)
		{
			Contract.Requires(method != null);
			Contract.Requires(typeof(F).IsSubclassOf(typeof(MulticastDelegate)));
			Contract.Requires(!method.IsGenericMethodDefinition);

			Type fReturnType = typeof(F).GetMethod("Invoke").ReturnType;
			shortCircuitToSignature = method.IsStatic && fReturnType == method.ReturnType;

			if (fReturnType != typeof(void) && method.ReturnType == typeof(void))
				Contract.Requires(method.ReturnType != typeof(void), $"A void-returning method was specified, but the delegate type does not return void");

			int returnTypeCount = fReturnType == typeof(void) ? 0 : 1;
			int expectedReceivedTypeParameterCount = method.GetParameters().Length + (method.IsStatic ? 0 : 1) + returnTypeCount;
			Contract.Requires(expectedReceivedTypeParameterCount == receivedTypeParameterCount, $"Incorrect number of type parameters specified. Got {receivedTypeParameterCount}, expected {expectedReceivedTypeParameterCount}. ");
			// TODO: check return type + parameter types compatibility. out/ref parameters?
		}

		/// <summary> Converts the method info to an action. </summary>
		public static Action ToAction(this MethodInfo method)
		{
			RequiresConveribleToSignature<Action>(method, 0, out bool shortCircuitToSignature);

			if (shortCircuitToSignature)
			{
				return method.ToSignature<Action>();
			}
			else
			{
				return () => method.Invoke(null, Array.Empty<object>());
			}
		}
		/// <summary> Converts the method info to an action. </summary>
		public static Action<T1> ToAction<T1>(this MethodInfo method)
		{
			RequiresConveribleToSignature<Action<T1>>(method, 1, out bool shortCircuitToSignature);

			if (shortCircuitToSignature)
			{
				return ToSignature<Action<T1>>(method);
			}
			if (method.IsStatic)
			{
				return arg => method.Invoke(null, new object[] { arg });
			}
			else
			{
				return target => method.Invoke(target, Array.Empty<object>());
			}
		}
		/// <summary> Converts the method info to an action. </summary>
		public static Action<T1, T2> ToAction<T1, T2>(this MethodInfo method)
		{
			RequiresConveribleToSignature<Action<T1, T2>>(method, 2, out bool shortCircuitToSignature);

			if (shortCircuitToSignature)
			{
				return ToSignature<Action<T1, T2>>(method);
			}
			if (method.IsStatic)
			{
				return (arg1, arg2) => method.Invoke(null, new object[] { arg1, arg2 });
			}
			else
			{
				return (target, arg) => method.Invoke(target, new object[] { arg });
			}
		}
		/// <summary> Converts the method info to a func. </summary>
		public static Func<TResult> ToFunc<TResult>(this MethodInfo method)
		{
			RequiresConveribleToSignature<Func<TResult>>(method, 1, out bool shortCirctuiToSignature);
			Contract.Assume(shortCirctuiToSignature);

			return ToSignature<Func<TResult>>(method);
		}
		/// <summary> Converts the method info to a func. </summary>
		public static Func<T1, TResult> ToFunc<T1, TResult>(this MethodInfo method)
		{
			RequiresConveribleToSignature<Func<T1, TResult>>(method, 2, out bool shortCircuitToSignature);

			if (shortCircuitToSignature)
			{
				return ToSignature<Func<T1, TResult>>(method);
			}
			else if (method.IsStatic)
			{
				return arg => (TResult)method.Invoke(null, new object[] { arg });
			}
			else
			{
				return target => (TResult)method.Invoke(target, Array.Empty<object>());
			}
		}
		/// <summary> Converts the method info to a func. </summary>
		public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(this MethodInfo method)
		{
			RequiresConveribleToSignature<Func<T1, T2, TResult>>(method, 3, out bool shortCircuitToSignature);

			if (shortCircuitToSignature)
			{
				return ToSignature<Func<T1, T2, TResult>>(method);
			}
			else if (method.IsStatic)
			{
				return (arg1, arg2) => (TResult)method.Invoke(null, new object[] { arg1, arg2 });
			}
			else
			{
				return (target, arg) => (TResult)method.Invoke(target, new object[] { arg });
			}
		}
		/// <summary>
		/// Creates a delegate representing the specified <see cref="MethodInfo"/>. 
		/// If it returns void, it is wrapped in a function that returns null.
		/// <seealso href="https://stackoverflow.com/a/4117437/308451"/>
		/// </summary>
		[DebuggerHidden]
		public static Func<object> ToDelegate(this MethodInfo method, object target = null)
		{
			if (method.ReturnType == typeof(void))
			{
				var action = (Action)Delegate.CreateDelegate(typeof(Action), target, method);
				return new WrapToNull(action).Wrap;
			}
			else
			{
				var delegateType = typeof(Func<object>);
				return (Func<object>)Delegate.CreateDelegate(delegateType, target, method);
			}
		}
		readonly struct WrapToNull
		{
			public Action Action { get; }
			[DebuggerHidden]
			public WrapToNull(Action action)
			{
				Contract.Requires(action != null);
				this.Action = action;
			}
			[DebuggerHidden]
			public object Wrap()
			{
				Action();
				return null;
			}
		}

		/// <summary> Wraps getting the static field value with a getter function. </summary>
		public static Func<TField> ToFunc<TField>(this FieldInfo staticField)
		{
			Contract.Requires(staticField != null);
			Contract.Requires(staticField.IsStatic, "Expected 2 type parameters for non-static field");
			Contract.Requires(typeof(TField) == staticField.FieldType);

			return () => (TField)staticField.GetValue(null);
		}
		/// <summary> Wraps setting the static field value with a getter function. </summary>
		public static Action<TField> ToAction<TField>(this FieldInfo staticField)
		{
			Contract.Requires(staticField != null);
			Contract.Requires(staticField.IsStatic, "Expected 2 type parameters for non-static field");
			Contract.Requires(typeof(TField) == staticField.FieldType);
			Contract.Requires(!staticField.IsInitOnly);

			return value => staticField.SetValue(null, value);
		}
		/// <summary> Wraps getting the non-static field value with a getter function. </summary>
		public static Func<TDeclaringType, TField> ToFunc<TDeclaringType, TField>(this FieldInfo nonstaticField)
		{
			Contract.Requires(nonstaticField != null);
			Contract.Requires(!nonstaticField.IsStatic, "Expected 1 type parameter for static field");
			Contract.Requires(typeof(TField).IsAssignableFrom(nonstaticField.FieldType));
			Contract.Requires(nonstaticField.DeclaringType.IsAssignableFrom(typeof(TDeclaringType)));
			Contract.Requires(!nonstaticField.IsInitOnly);

			return target => (TField)nonstaticField.GetValue(target);
		}
		/// <summary> Wraps setting the non-static field value with a setter function. </summary>
		public static Action<TDeclaringType, TField> ToAction<TDeclaringType, TField>(this FieldInfo nonstaticField)
		{
			Contract.Requires(nonstaticField != null);
			Contract.Requires(!nonstaticField.IsStatic, "Expected 1 type parameter for static field");
			Contract.Requires(typeof(TField).IsAssignableFrom(nonstaticField.FieldType));

			return (target, value) => nonstaticField.SetValue(target, value);
		}

        /// <summary> Gets whether the specified type implements the specified interface type, including all (if any) generic type arguments. Does not take into account variance. </summary>
        /// <param name="type"> The type to check whether it implements the interface. </param>
        /// <param name="interfaceType"> The interface to check whether it is implemented. </param>
        [DebuggerHidden]
        public static bool Implements(this Type type, Type interfaceType)
		{
			Contract.Requires(type != null);
			Contract.Requires(interfaceType != null);
			Contract.Requires(interfaceType.IsInterface);

			if (interfaceType.IsGenericTypeDefinition)
			{
				return type.GetInterfaces()
						   .Where(candidate => candidate.IsGenericType)
						   .Select(candidate => candidate.GetGenericTypeDefinition())
						   .Any(candidate => candidate == interfaceType);
			}
			else
			{
				return type.GetInterfaces().Any(candidate => candidate == interfaceType);
			}
		}

		/// <summary>
		/// Yields the specified type and all its base types.
		/// </summary>
		[DebuggerHidden]
		public static IEnumerable<Type> GetBaseTypesAndSelf(this Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			yield return type;
			if (type.BaseType != null)
				foreach (var result in GetBaseTypesAndSelf(type.BaseType))
					yield return result;
		}
		/// <summary>
		/// Gets whether the specified property represents an indexer;
		/// </summary>
		public static bool IsIndexer(this PropertyInfo property)
		{
			Contract.Requires(property != null);

			return property.GetIndexParameters().Length > 0;
		}


		/// <summary>
		/// Invokes the getter of the property defined on the specified interface implemented by the specified object.
		/// </summary>
		public static T GetPropertyValue<T>(this Type interfaceType, string propertyName, object obj)
		{
			Contract.Requires(obj != null);
			Contract.Requires(interfaceType != null);
			Contract.Requires(!string.IsNullOrEmpty(propertyName));
			Contract.Requires(interfaceType.IsInterface, "The specified type is not an interface");
			Contract.Requires(obj.GetType().Implements(interfaceType), "The specified object does not implement the specified interface");
			Contract.Requires(interfaceType.GetProperty(propertyName) != null, $"No property found on interface '{interfaceType}' with the name '{propertyName}");
			Contract.Requires(interfaceType.GetProperty(propertyName).GetMethod != null, "The specified property is write-only");

			object typeunsafeResult = interfaceType.GetProperty(propertyName).GetMethod.Invoke(obj, EmptyCollection<object>.Array);
			T result = (T)typeunsafeResult;
			return result;
		}

		/// <summary>
		///  When overridden in a derived class, returns the System.Reflection.MethodInfo
		///  object for the method on the direct or indirect base class in which the method
		///  represented by this instance was first declared. Otherwise null is returned.
		/// </summary>
		public static PropertyInfo GetBaseDefinition(this PropertyInfo propertyInfo)
		{
			Contract.Requires(propertyInfo != null);

			var accessor = propertyInfo.GetGetMethod(true);
			if (accessor == null)
				accessor = propertyInfo.GetSetMethod(true);
			if (accessor == null)
				throw new Exception("Dunno");

			var baseType = accessor.GetBaseDefinition()?.DeclaringType;
			if (baseType == null)
				return null;
			var baseProperty = baseType.GetProperty(propertyInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
			Contract.Ensures(baseProperty.DeclaringType != propertyInfo.DeclaringType);
			return baseProperty;
		}
		/// <summary>
		/// Gets whether the specified type is static.
		/// </summary>
		[DebuggerHidden]
		public static bool IsStatic(this Type type)
		{
			Contract.Requires(type != null);

			return type.IsAbstract && type.IsSealed;
		}

		/// <summary> Gets whether the specified type is an anonymous type. </summary>
		/// <see href="https://stackoverflow.com/a/2483054/308451"/>
		[DebuggerHidden]
		public static bool IsAnonymous(this Type type)
		{
			Contract.Requires(type != null);

			return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
			   && type.IsGenericType && type.Name.Contains("AnonymousType")
			   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
			   && type.Attributes.HasFlag(TypeAttributes.NotPublic);
		}
		/// <summary> Gets all hierarchy-flattened properties on the specified interface.  </summary> 
		public static IEnumerable<PropertyInfo> GetFlattenedInterfaceProperties(this Type interfaceType)
		{
			Contract.Requires(interfaceType != null);
			Contract.Requires(interfaceType.IsInterface);

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public;
			return interfaceType.ToSingleton()
								.Concat(interfaceType.GetInterfaces())
								.SelectMany(i => i.GetProperties(flags));
		}

		/// <summary> Gets all hierarchy-flattened properties on the specified type, which could be an interface.  </summary> 
		public static IEnumerable<PropertyInfo> GetFlattenedProperties(this Type type)
		{
			Contract.Requires(type != null);

			if (type.IsInterface)
				return type.GetFlattenedInterfaceProperties();

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public;
			return type.GetProperties(flags);
		}
#nullable enable
		/// <summary>
		/// Similar to <see cref="Type.IsAssignableFrom(Type)" />, except that it also allows for the type being assigned to generic type definitions.
		/// </summary>
		public static bool IsOpenlyAssignableFrom(this Type lhs, Type type)
		{
			return lhs.IsOpenlyAssignableFrom(type, out var _);
		}
		/// <summary>
		/// Similar to <see cref="Type.IsAssignableFrom(Type)" />, except that it also allows for the type being assigned to generic type definitions.
		/// </summary>
		public static bool IsOpenlyAssignableFrom(this Type lhs, Type type, out Type[] typeParameters)
		{
			if (!lhs.IsGenericTypeDefinition)
			{
				typeParameters = Array.Empty<Type>();
				return lhs.IsAssignableFrom(type);
			}


			if (type.IsGenericType && type.GetGenericTypeDefinition() == lhs)
			{
				typeParameters = type.GetGenericArguments();
				return true;
			}


			foreach (Type i in type.GetInterfaces())
			{
				if (i.IsGenericType && i.GetGenericTypeDefinition() == lhs)
				{
					typeParameters = i.GetGenericArguments();
					return true;
				}
			}

			if (type.BaseType == null)
			{
				typeParameters = Array.Empty<Type>();
				return false;
			}

			return lhs.IsOpenlyAssignableFrom(type.BaseType, out typeParameters);
		}
		/// <summary>
		/// Gets whether the specified methodinfo or type is abstract.
		/// </summary>
		[DebuggerHidden]
		public static bool IsAbstract(this MemberInfo member)
		{
			return member switch
			{
				null => throw new ArgumentNullException(nameof(member)),
				MethodInfo mi => mi.IsAbstract,
				Type t => t.IsAbstract,
				_ => throw new ArgumentException()
			};
		}

		/// <summary>
		/// Finds all types in all loaded assemblies that have the specified unqualified name.
		/// </summary>
		[DebuggerHidden]
		public static IEnumerable<Type> FindType(string unqualifiedName)
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				                          .Reverse()
				                          .FindType(unqualifiedName);
		}
		/// <summary>
		/// Finds all types in all loaded assemblies that have the specified unqualified name.
		/// </summary>
		[DebuggerHidden]
		public static IEnumerable<Type> FindType(this IEnumerable<Assembly> assemblies, string fullyQualifiedName)
		{
			var typeName = fullyQualifiedName.SubstringAfterLast(".");
			var @namespace = typeName.Length == fullyQualifiedName.Length ? null : fullyQualifiedName.SubstringUntilLast(".");
			foreach (var assembly in assemblies)
			{
				Type[] types;
				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException)
				{
					// https://stackoverflow.com/a/67976906/308451
					continue;
				}
				foreach (var type in types)
				{
					if (type.Name == typeName && (@namespace == null || @namespace == type.Namespace))
					{
						yield return type;
					}
				}
			}
		}
	}
}
