#nullable disable 
using JBSnorro;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace JBSnorro;

public static class EventBindingExtensions
{

	/// <summary> Invokes the specified handler when a property on a property changes, or when the source changes, 
	/// and, if the current value differs from the default value, at the end of this call. </summary>
	/// <param name="propertyName"> The name of the property on the source. </param>
	/// <param name="propertyOnPropertyName"> The name of the property on the property on the source. </param>
	public static void Bind(this INotifyPropertyChanged source, string propertyName, string propertyOnPropertyName, Action handler)
	{
		Bind(source, propertyName, propertyOnPropertyName, (sender, e) => handler(), null);
	}
	/// <summary> Invokes the specified handler when a property on a property changes, or when the source changes, 
	/// and, if the current value differs from the default value, at the end of this call. </summary>
	/// <param name="propertyName"> The name of the property on the source. </param>
	/// <param name="propertyOnPropertyName"> The name of the property on the property on the source. </param>
	/// <param name="handler"> The handler to be invoked at the moments listed above. </param>
	public static void Bind(this INotifyPropertyChanged source, string propertyName, string propertyOnPropertyName, PropertyChangedEventHandler handler)
	{
		// this overload makes use of the feature of the other overload to correct the default value if it is null to the default
		Bind(source, propertyName, propertyOnPropertyName, handler, null);
	}
	/// <summary> Invokes the specified handler when a property on a property changes, or when the source changes, 
	/// and, if the current value differs from the default value, at the end of this call. </summary>
	/// <param name="propertyName"> The name of the property on the source. </param>
	/// <param name="propertyOnPropertyName"> The name of the property on the property on the source. </param>
	/// <param name="handler"> The handler to be invoked at the moments listed above. </param>
	public static void Bind<T>(this INotifyPropertyChanged source, string propertyName, string propertyOnPropertyName, PropertyMutatedEventHandler<T> handler)
	{
		// this overload makes use of the feature of the other overload to correct the default value if it is null to the default
		Bind(source, propertyName, propertyOnPropertyName, (sender, e) => handler(sender, (PropertyMutatedEventArgs<T>)e), null);
	}

	/// <summary> Invokes the specified handler when a property on a property changes, or when the source changes, 
	/// and, if the current value differs from the specified default value, at the end of this call. </summary>
	/// <param name="propertyName"> The name of the property on the source. </param>
	/// <param name="propertyOnPropertyName"> The name of the property on the property on the source. </param>
	/// <param name="handler"> The handler to be invoked at the moments listed above. </param>
	/// <param name="defaultValue"> The value that is the default of the property on the property (used as old value when the property containing that property is null). </param>
	public static void Bind<T>(this INotifyPropertyChanged source, string propertyName, string propertyOnPropertyName, PropertyMutatedEventHandler<T> handler, T defaultValue)
	{
		Bind(source, propertyName, propertyOnPropertyName, (sender, e) => handler(sender, (PropertyMutatedEventArgs<T>)e), defaultValue, typeof(T));
	}
	/// <summary> Invokes the specified handler when a property on a property changes, or when the source changes, 
	/// and, if the current value differs from the specified default value, at the end of this call. </summary>
	/// <param name="propertyName"> The name of the property on the source. </param>
	/// <param name="propertyOnPropertyName"> The name of the property on the property on the source. </param>
	/// <param name="handler"> The handler to be invoked at the moments listed above. </param>
	/// <param name="defaultValue"> The value that is the default of the property on the property (used as old value when the property containing that property is null). 
	/// For value types, you can specify null to indicate the default of the value type. </param>
	public static void Bind(this INotifyPropertyChanged source, string propertyName, string propertyOnPropertyName, PropertyChangedEventHandler handler, object defaultValue)
	{
		Contract.Requires(source != null);
		Contract.Requires(source.GetType().GetProperty(propertyName) != null);

		var sourcePropertyType = source.GetType().GetProperty(propertyName).PropertyType;
		Type propertyOnPropertyType = GetPropertyType(sourcePropertyType, propertyOnPropertyName);

		if (ReferenceEquals(defaultValue, null))
		{
			defaultValue = propertyOnPropertyType.GetDefault();
		}
		else
		{
			Contract.Requires(propertyOnPropertyType.IsInstanceOfType(defaultValue));
		}

		Bind(source, propertyName, propertyOnPropertyName, handler, defaultValue, propertyOnPropertyType);
	}
	/// <summary> Implements the functionality documented in the other overloads. </summary>
	private static void Bind(INotifyPropertyChanged source, string propertyName, string propertyOnPropertyName, PropertyChangedEventHandler handler, object defaultValue, Type propertyOnPropertyType)
	{
		Contract.Requires(source != null);
		Contract.Requires(handler != null);
		Contract.Requires(source.GetType().GetProperty(propertyName) != null);
		var sourceProperty = GetProperty(source.GetType(), propertyName);
		var sourcePropertyType = GetPropertyType(source.GetType(), propertyName);
		Contract.Requires(sourcePropertyType.Implements(typeof(INotifyPropertyChanged)), $"The property on the source must implement '{nameof(INotifyPropertyChanged)}'");
		Contract.Requires(GetPropertyType(sourcePropertyType, propertyOnPropertyName) != null, $"Property '{propertyName}' was not found on type '{sourcePropertyType.Name}'");
		Contract.Requires(propertyOnPropertyType.IsAssignableFrom(GetPropertyType(sourcePropertyType, propertyOnPropertyName)));
		Contract.Requires(propertyOnPropertyType.IsInstanceOfType(defaultValue) || (ReferenceEquals(defaultValue, null) && !propertyOnPropertyType.IsValueType));

		handler = handler.FilterOn(propertyOnPropertyName);
		PropertyChangedEventHandler onSourcePropertyChanged = (sender, eventArg) =>
		{
			Contract.Requires(eventArg is IPropertyMutatedEventArgs);

			var e = (IPropertyMutatedEventArgs)eventArg;

			var oldSource = (INotifyPropertyChanged)e.OldValue;
			if (oldSource != null)
			{
				oldSource.PropertyChanged -= handler;
			}

			var newSource = (INotifyPropertyChanged)e.NewValue;
			if (newSource != null)
			{
				newSource.PropertyChanged += handler;
			}

			var oldValue = GetPropertyValue(oldSource, propertyOnPropertyName, defaultValue: defaultValue);
			var newValue = GetPropertyValue(newSource, propertyOnPropertyName, defaultValue: defaultValue);
			if (!Equals(oldValue, newValue)) // uses virtual method oldValue.Equals(object)
			{
				handler(newSource ?? oldSource, PropertyMutatedEventArgsExtensions.Create(propertyOnPropertyName, propertyOnPropertyType, oldValue, newValue));
			}
		};

		source.PropertyChanged += onSourcePropertyChanged.FilterOn(propertyName);

		object currentValue = sourceProperty.GetValue(source);
		// onSourcePropertyChanged is invoked regardless of whether currentValue equals sourcePropertyType.GetDefault()
		// because it hooks the handler onto currentValue.PropertyChange
		onSourcePropertyChanged(source, PropertyMutatedEventArgsExtensions.Create(propertyName, sourcePropertyType, sourcePropertyType.GetDefault(), currentValue));
	}
	/// <summary> Invokes the specified handler when a collection property on a property changes, when the source changes, 
	/// or when the collection changes. </summary>
	/// <param name="propertyName"> The name of the property on the source. </param>
	/// <param name="collectionPropertyOnPropertyName"> The name of the collection property on the property on the source, i.e. <code>source.property.collectionProperty</code></param>
	/// <param name="handler"> The handler to be invoked at the moments listed above. </param>
	public static void BindCollection(this INotifyPropertyChanged source, string propertyName, string collectionPropertyOnPropertyName, NotifyCollectionChangedEventHandler handler)
	{
		Contract.Requires(GetPropertyType(source.GetType(), propertyName, collectionPropertyOnPropertyName).Implements(typeof(INotifyCollectionChanged)), $"The collection property '{collectionPropertyOnPropertyName}' on the source must implement '{nameof(INotifyCollectionChanged)}'");

		source.Bind(propertyName, collectionPropertyOnPropertyName, RebindCollectionChangedHandler);

		void RebindCollectionChangedHandler(object sender, PropertyChangedEventArgs eventArg)
		{
			IPropertyMutatedEventArgs e = (IPropertyMutatedEventArgs)eventArg;
			Contract.Requires(sender != null);
			Contract.Requires(e != null);
			Contract.Requires(e.OldValue == null || e.OldValue.GetType().Implements(typeof(INotifyCollectionChanged)), "The collection instance must implement 'INotifyCollectionChanged'");
			Contract.Requires(e.NewValue == null || e.NewValue.GetType().Implements(typeof(INotifyCollectionChanged)), "The collection instance must implement 'INotifyCollectionChanged'");
			Contract.Requires(e.OldValue == null || e.OldValue.GetType().Implements(typeof(System.Collections.IList)), "The collection instance must implement 'IList'");
			Contract.Requires(e.NewValue == null || e.NewValue.GetType().Implements(typeof(System.Collections.IList)), "The collection instance must implement 'IList'");

			if (e.OldValue is INotifyCollectionChanged oldCollection)
			{
				oldCollection.CollectionChanged -= handler;
			}
			if (e.NewValue is INotifyCollectionChanged newCollection)
			{
				newCollection.CollectionChanged += handler;
				handler(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newCollection, e.OldValue as System.Collections.IList));
			}
		}
	}

	/// <summary> Restricts invocation of the handler to when the property name matches. </summary>
	private static PropertyChangedEventHandler FilterOn(this PropertyChangedEventHandler handler, string propertyName)
	{
		Contract.Requires(handler != null);
		Contract.Requires(propertyName != null);

		return (sender, e) =>
			   {
				   if (e.PropertyName == propertyName)
				   {
					   handler(sender, e);
				   }
			   };
	}



	/// <summary> Gets the value of the specified property on the specified object. </summary>
	/// <param name="defaultValue"> Used when the specified source is null. </param>
	internal static T GetPropertyValue<T>(object source, string propertyName, Option<T> defaultValue = default)
	{
		return GetPropertyValue<T>(source, propertyName, source?.GetType(), defaultValue);
	}
	/// <summary> Gets the value of the specified property on the specified object accessible via the specified type (an interface). </summary>
	/// <param name="defaultValue"> Used when the specified source is null. </param>
	internal static T GetPropertyValue<TSourceInterface, T>(TSourceInterface source, string propertyName, Option<T> defaultValue = default)
	{
		return GetPropertyValue<T>(source, propertyName, typeof(TSourceInterface), defaultValue);
	}
	/// <summary> Gets the value of the specified property on the specified object. </summary>
	/// <param name="defaultValue"> Used when the specified source is null. </param>
	/// <param name="sourceType"> The type of the source on which the property can be found. Generally the type of the source, but interfaces are possible too. </param>
	private static T GetPropertyValue<T>(object source, string propertyName, Type sourceType, Option<T> defaultValue)
	{
		if (source != null)
		{
			Type propertyType = sourceType.GetProperty(propertyName)?.PropertyType;
			Contract.Requires(propertyType != null, $"Could not find a property with name '{propertyName}' on type '{sourceType}'");
			Contract.Requires(propertyType.IsAssignableFrom(typeof(T)), $"The type of the property with name '{propertyName}' is incompatible with the specified type '{typeof(T).Name}'");
		}
		return (T)GetPropertyValue(source, propertyName, sourceType, defaultValue.HasValue ? new Option<object>(defaultValue.Value) : default);
	}
	/// <summary> Gets the value of the specified property on the specified object. </summary>
	/// <param name="defaultValue"> Used when the specified source is null. </param>
	internal static object GetPropertyValue(object source, string propertyName, Type sourceType = null, Option<object> defaultValue = default)
	{
		Contract.Requires(source != null || defaultValue.HasValue, $"The property value could not be obtained: '{nameof(source)}' was null");

		if (source == null)
		{
			return defaultValue.Value;
		}
		else
		{
			sourceType = sourceType ?? source.GetType();

			var property = GetProperty(sourceType, propertyName);
			var result = property.GetValue(source);
			return result;
		}
	}



	/// <summary>
	/// Gets whether the specified type has an instance property of the specified name.
	/// </summary>
	internal static bool HasProperty<TSource>(string propertyName)
	{
		return HasProperty(propertyName, typeof(TSource));
	}
	/// <summary>
	/// Gets whether the specified type has an instance property of the specified name.
	/// </summary>
	internal static bool HasProperty(string propertyName, Type sourceType)
	{
		Contract.Requires(!string.IsNullOrEmpty(propertyName));
		Contract.Requires(sourceType != null);

		return sourceType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) != null;
	}
	/// <summary> Gets the type of the specified property on the specified type. </summary>
	internal static Type GetPropertyType(this Type sourceType, string propertyName)
	{
		Contract.Requires<ArgumentNullException>(sourceType != null);

		return GetProperty(sourceType, propertyName)?.PropertyType;
	}
	/// <summary> Gets the type of the property on the property on the specified type. </summary>
	internal static Type GetPropertyType(this Type sourceType, string propertyName, string propertyOnPropertyName)
	{
		Contract.Requires<ArgumentNullException>(sourceType != null);

		var property = GetProperty(sourceType, propertyName);
		Contract.Assert(property != null, $"Could not find property '{propertyName}'");

		return GetProperty(property.PropertyType, propertyOnPropertyName)?.PropertyType;
	}
	internal static PropertyInfo GetProperty(this Type sourceType, string propertyName)
	{
		Contract.Requires<ArgumentNullException>(sourceType != null);

		return sourceType.GetProperties().FirstOrDefault(property => property.Name == propertyName);
	}

	/// <summary>
	/// Invokes the specified handler for each change of the specified property on any of the elements in the specified collection.
	/// Note that the handler is not invoked for elements already present, nor if items are added where the property already has a value.
	/// </summary>
	/// <param name="collection"> The collection of elements on which to monitor property changes. </param>
	/// <param name="handler"> The action invoked when the property changes. </param>
	/// <param name="propertyName"> The name of the property to monitor. </param>
	/// <param name="handleNewItems"> Indicates whether the relevant property on an item added to the collection should be treated as though it changed. </param>
	public static void Bind(this INotifyCollectionChanged collection,
							string propertyName,
							PropertyChangedEventHandler handler,
							Type elementType = null,
							bool handleNewItems = false,
							PropertyChangedEventHandler onRemove = null)
	{
		Contract.Requires(collection != null);
		Contract.Requires(collection is System.Collections.IEnumerable);
		Contract.Requires(elementType == null || HasProperty(propertyName, elementType));


		collection.CollectionChanged += collectionChanged;
		var collectionList = collection as System.Collections.IList ?? ((System.Collections.IEnumerable)collection).Cast<object>().ToList();
		if (collectionList.Count != 0)
		{
			elementType = elementType ?? collectionList[0].GetType();
			Contract.Requires(HasProperty(propertyName, elementType));

			collectionChanged(collection, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collectionList));
		}


		void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Move)
				return;

			if (e.NewItems != null)
			{
				foreach (var newItem in e.NewItems)
				{
					Contract.Requires(newItem is INotifyPropertyChanged, "This binding requires that all elements be non-null and implement INotifyPropertyChanged");
					((INotifyPropertyChanged)newItem).PropertyChanged += propertyChanged;
					if (handleNewItems)
						handler(newItem, PropertyMutatedEventArgsExtensions.From(newItem, propertyName, elementType));
				}
			}
			if (e.OldItems != null)
			{
				foreach (var newItem in e.OldItems)
				{
					((INotifyPropertyChanged)newItem).PropertyChanged -= propertyChanged;
					onRemove?.Invoke(newItem, PropertyMutatedEventArgsExtensions.From(newItem, propertyName, GetPropertyValue(newItem, propertyName, elementType), newValue: null, elementType));
				}
			}
		}
		void propertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == propertyName)
			{
				handler(sender, e);
			}
		}
	}

	/// <summary>
	/// Binds the property on many elements into a combination for which <paramref name="handler"/> is invoked whenever its value changes. 
	/// This is useful for instance when you have a property on a collection that is the reduction of a property of collection element (in which case you can use the handler to set the reduced value).
	/// </summary>
	/// <typeparam name="TElement"> The type of the elements of the specified observable collection. </typeparam>
	/// <typeparam name="TElementProperty"> The type of the property on <typeparamref name="TElement"/> whose name is <paramref name="propertyName"/> and whose values are to be reduced. </typeparam>
	/// <param name="collection"> The collection to monitor for property changes. </param>
	/// <param name="propertyName"> The name of the property on the collection elements to monitor. </param>
	/// <param name="handler"> The function handling the change of the reduced value. The first argument is the collection and the second the new value. </param>
	/// <param name="combine"> The function taking the previous reduced value and an updated value of one of the properies, and returns their reduction. </param>
	/// <param name="valueIfCollectionEmpty"> The value to be taken if the specified collection is empty. </param>
	/// <param name="equalityComparer"> The function comparing <typeparamref name="TElementProperty"/>s for equality, determining when to invoke <paramref name="handler"/>. </param>
	public static void BindCollective<TElement, TElementProperty>(this INotifyCollectionChanged collection,
																  string propertyName,
																  PropertyChangedEventHandler handler,
																  Func<TElementProperty/*previous resultant*/, TElementProperty /*new property*/, TElementProperty> combine,
																  Option<TElementProperty> valueIfCollectionEmpty = default,
																  IEqualityComparer<TElementProperty> equalityComparer = null)
	{
		Contract.Requires(collection != null);
		Contract.Requires(collection is IEnumerable<TElement>);
		Contract.Requires(HasProperty<TElement>(propertyName));
		Contract.Requires(handler != null);
		Contract.Requires(combine != null);

		equalityComparer = equalityComparer ?? EqualityComparer<TElementProperty>.Default;

		Option<TElementProperty> resultant = Option<TElementProperty>.None;
		collection.Bind(propertyName, elementPropertyChanged, typeof(TElement), true, onElementRemoved);
		setResultant(recomputeResultant()); // if handler shouldn't be triggered on the initially present elements, set as resultant = recomputeResultant() rather than through setResultant


		Option<TElementProperty> recomputeResultant()
		{
			Option<TElementProperty> result = Option<TElementProperty>.None;
			foreach (var element in (IEnumerable<TElement>)collection)
			{
				var propValue = GetPropertyValue<TElement, TElementProperty>(element, propertyName);

				if (!result.HasValue)
				{
					result = propValue;
				}
				else
				{
					result = combine(result.Value, propValue);
				}
			}
			return result; // returns Option.None when the collection is empty
		}

		void onElementRemoved(object sender, PropertyChangedEventArgs e)
		{
			Contract.Assert(resultant.HasValue);

			var propValue = GetPropertyValue<TElement, TElementProperty>((TElement)sender, e.PropertyName);

			if (equalityComparer.Equals(propValue, resultant.Value)) // if the resultant value was removed from the collection
			{
				setResultant(recomputeResultant());
			}
		}

		void elementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == propertyName)
			{
				var newValue = GetPropertyValue<TElement, TElementProperty>((TElement)sender, e.PropertyName);
				var newResultant = resultant.HasValue ? combine(resultant.Value, newValue) : newValue;
				setResultant(newResultant);
			}
		}

		void setResultant(Option<TElementProperty> newResultant)
		{
			// newResultant: None means the collection is empty.
			TElementProperty newValue;
			if (newResultant.HasValue)
			{
				newValue = newResultant.Value;
			}
			else
			{
				if (valueIfCollectionEmpty.HasValue)
				{
					newValue = valueIfCollectionEmpty.Value;
				}
				else
				{
					// don't trigger the handler if there is no value to set
					return;
				}
			}

			if (resultant.HasValue)
			{
				TElementProperty oldValue = resultant.Value;
				if (!equalityComparer.Equals(oldValue, newValue))
				{
					handle(oldValue, newValue);
				}
			}
			else
			{
				TElementProperty oldValue = valueIfCollectionEmpty.ValueOrDefault();
				handle(oldValue, newValue);
			}
		}

		void handle(TElementProperty oldValue, TElementProperty newValue)
		{
			var arg = new PropertyMutatedEventArgs<TElementProperty>(propertyName, oldValue, newValue);
			handler(collection, arg);
		}
	}

	/// <summary>
	/// Binds an event handler to a property on an <see cref="INotifyPropertyChanged"/>. Updates the attachedness of the handler when the property changes.
	/// </summary>
	/// <typeparam name="TEventArgs"></typeparam>
	/// <param name="source"> The object that has the property to be monitored. </param>
	/// <param name="propertyName"> The name of the property on the source. </param>
	/// <param name="eventName">  The name of the event on the property on the source. </param>
	/// <param name="eventHandler"> The handler to be invoked when the event on the current property value fires. </param>
	public static void BindEvent<TEventArgs>(this INotifyPropertyChanged source, string propertyName, string eventName, Action<object, TEventArgs> eventHandler)
		where TEventArgs : EventArgs
	{
		Contract.Requires(source != null);
		Contract.Requires(!string.IsNullOrEmpty(eventName));
		var propertyType = GetPropertyType(source.GetType(), propertyName);
		Contract.Requires(propertyType != null);
		var eventInfo = propertyType.GetEvent(eventName);
		Contract.Requires(eventInfo != null);
		Contract.Requires(eventInfo.HasHandlerSignature<TEventArgs>());
		Contract.Requires(eventHandler != null);

		source.PropertyChanged += sourcePropertyChanged;

		sourcePropertyChanged(source, PropertyMutatedEventArgsExtensions.From(source, propertyName));

		void sourcePropertyChanged(object sender, PropertyChangedEventArgs e_)
		{
			if (e_.PropertyName == propertyName)
			{
				var e = (IPropertyMutatedEventArgs)e_;

				if (e.OldValue != null)
				{
					eventInfo.RemoveEventHandler(e.OldValue, eventHandler);
				}
				if (e.NewValue != null)
				{
					eventInfo.AddEventHandler(e.NewValue, eventHandler);
				}
			}
		}
	}

	/// <summary>
	/// Binds the specified <see cref="PropertyChangedEventHandler"/> on _all_ observables (including via <see cref="INotifyCollectionChanged"/>) 
	/// reachable via reflection that have a property with the specified name and type, and invokes the handler on those event that match the predicate. 
	/// </summary>
	public static IDisposable BindRecursively<TPropertyType>(this INotifyPropertyChanged root,
															 string propertyName,
															 PropertyMutatedEventHandler<TPropertyType> handler)
	{
		return root.BindRecursively(predicate, handlerWrapper);

		bool predicate(object obj, IPropertyMutatedEventArgs e)
		{
			if (e.PropertyName == propertyName)
			{
				var propertyType = GetPropertyType(obj.GetType(), propertyName);
				var result = typeof(TPropertyType).IsAssignableFrom(propertyType);
				return result;
			}
			return false;
		}
		void handlerWrapper(object sender, IPropertyMutatedEventArgs e)
		{
			handler(sender, (PropertyMutatedEventArgs<TPropertyType>)e);
		}
	}
	/// <summary>
	/// Binds a <see cref="PropertyChangedEventHandler"/> to all elements on the specified collection.
	/// </summary>
	/// <returns> a disposble which unhooks the <paramref name="handler"/> from all elements. </returns>
	public static IDisposable BindElements<TCollection, T>(this TCollection collection, PropertyChangedEventHandler handler) where T : INotifyPropertyChanged where TCollection : INotifyCollectionChanged, IEnumerable<T>
	{
		Contract.Requires(collection != null);
		Contract.Requires(handler != null);

		var collectionHandler = CollectionChangedEventHandler<T>.Create<TCollection>(add: add, remove: remove);
		collection.CollectionChanged += collectionHandler.CollectionChanged;
		return new Disposable(() =>
		{
			collection.CollectionChanged -= collectionHandler.CollectionChanged;

			foreach (T element in collection)
			{
				element.PropertyChanged -= handler;
			}
		});

		void add(TCollection sender, IReadOnlyList<T> newElements, int index)
		{
			Contract.Assert(ReferenceEquals(collection, sender));

			foreach (T element in newElements)
			{
				element.PropertyChanged += handler;
			}
		}
		void remove(TCollection sender, IReadOnlyList<T> oldElements, int index)
		{
			Contract.Assert(ReferenceEquals(collection, sender));

			foreach (T element in oldElements)
			{
				element.PropertyChanged -= handler;
			}
		}
	}


	/// <summary>
	/// Binds a <see cref="NotifyCollectionChangedEventHandler"/> to all collections by the name <paramref name="collectionPropertyName"/> on the elements of type <typeparamref name="T"/> on the specified collection.
	/// Note: not all elements of <paramref name="collection"/> are considered; only those of type <typeparamref name="T"/>. You could specify <see cref="INotifyPropertyChanged"/> to revert this.
	/// </summary>
	/// <returns> a disposble which unhooks the <paramref name="handler"/> from all elements. </returns>
	public static IDisposable BindElements<TCollection, T>(
		this TCollection collection,
		string collectionPropertyName,
		NotifyCollectionChangedEventHandler handler) where T : INotifyPropertyChanged where TCollection : INotifyCollectionChanged, IEnumerable<object>
	{
		Contract.Requires(collection != null);
		Contract.Requires(handler != null);

		PropertyChangedEventHandler intermediate = (element, e) =>
		{
			if (e.PropertyName == collectionPropertyName)
			{
				var _e = (IPropertyMutatedEventArgs)e;
				if (_e.OldValue is INotifyCollectionChanged oldCollection)
				{
					oldCollection.CollectionChanged -= handler;
				}
				if (_e.NewValue is INotifyCollectionChanged newCollection)
				{
					newCollection.CollectionChanged += handler;
				}
			}
		};


		var collectionHandler = CollectionChangedEventHandler<object>.Create<TCollection>(add: add, remove: remove);
		collection.CollectionChanged += collectionHandler.CollectionChanged;
		return new Disposable(() =>
		{
			collection.CollectionChanged -= collectionHandler.CollectionChanged;

			foreach (T element in collection)
			{
				element.PropertyChanged -= intermediate;
			}
		});

		void add(TCollection sender, IReadOnlyList<object> newElements, int index)
		{
			Contract.Assert(ReferenceEquals(collection, sender));

			foreach (T element in newElements.OfType<T>())
			{
				element.PropertyChanged += intermediate;
				intermediate(element, PropertyMutatedEventArgsExtensions.From(element, collectionPropertyName));
			}
		}
		void remove(TCollection sender, IReadOnlyList<object> oldElements, int index)
		{
			Contract.Assert(ReferenceEquals(collection, sender));

			foreach (T element in oldElements.OfType<T>())
			{
				element.PropertyChanged -= intermediate;
			}
		}
	}
	/// <summary>
	/// Binds the specified <see cref="PropertyChangedEventHandler"/> on _all_ observables (including via <see cref="INotifyCollectionChanged"/>) 
	/// reachable via reflection, and invokes the handler on those event that match the predicate. 
	/// </summary>
	/// <param name="predicate"> A predicate for invoking the handler. (not a predicate for hooking the handler). </param>
	public static IDisposable BindRecursively(this INotifyPropertyChanged root,
										  Func<object, IPropertyMutatedEventArgs, bool> predicate,
										  PropertyMutatedEventHandler handler)
	{
		Contract.Requires(root != null);
		Contract.Requires(predicate != null);
		Contract.Requires(handler != null);

		var allHookedObservables = new Dictionary<object, int>(ReferenceEqualityComparer.Instance); // integer is reference counts by this dictionary
		addReference(root);

#if DEBUG
		return new BindRecursivelyDisposable(allHookedObservables, () =>
#else
		return new Disposable(() =>
#endif
		{
			foreach (var obj in allHookedObservables)
				unhook(obj);
			allHookedObservables.Clear();
		});



		void hook(object tree)
		{
			foreach (object obj in getEntireObjectTree(tree))
			{
				var deltaReferenceCount = 0;
				if (obj is INotifyPropertyChanged container)
				{
					container.PropertyChanged += handle;
					deltaReferenceCount++;
				}
				if (obj is INotifyCollectionChanged collection)
				{
					collection.CollectionChanged += handleCollectionChanged;
					deltaReferenceCount++;
				}
				if (deltaReferenceCount != 0)
				{
					allHookedObservables[obj] = allHookedObservables.GetOrAdd(obj, 0) + deltaReferenceCount;
				}
			}
		}
		void unhook(object tree)
		{
			Contract.Assume(allHookedObservables.ContainsKey(tree));
			foreach (object obj in getEntireObjectTree(tree))
			{
				var deltaReferenceCount = 0;
				if (obj is INotifyPropertyChanged container)
				{
					container.PropertyChanged -= handle;
					deltaReferenceCount--;
				}
				if (obj is INotifyCollectionChanged collection)
				{
					collection.CollectionChanged -= handleCollectionChanged;
					deltaReferenceCount--;
				}
				if (deltaReferenceCount != 0)
				{
					allHookedObservables[obj] += deltaReferenceCount;
				}

			}
		}

		void addReference(object newReferredObject)
		{
			if (newReferredObject != null && allHookedObservables.ContainsKey(newReferredObject))
			{
				allHookedObservables[newReferredObject]++;
			}
			else
			{
				hook(newReferredObject);
			}
		}
		void removeReference(object oldReferredObject)
		{
			if (oldReferredObject != null && allHookedObservables.ContainsKey(oldReferredObject))
			{
				if (--allHookedObservables[oldReferredObject] == 0)
				{
					unhook(oldReferredObject);
				}
			}
		}
		void updateReferences(object oldReferredObject, object newReferredObject)
		{
			if (ReferenceEquals(oldReferredObject, newReferredObject))
			{
				return;
			}

			addReference(newReferredObject);
			removeReference(oldReferredObject);
		}

		void handle(object sender, PropertyChangedEventArgs originalE)
		{
			if (originalE is IPropertyMutatedEventArgs e)
			{
				updateReferences(e.OldValue, e.NewValue);
				if (predicate(sender, e))
				{
					handler.Invoke(sender, e);
				}
			}
		}

		void handleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (var newObj in e.NewItems)
						addReference(newObj);
					break;
				case NotifyCollectionChangedAction.Reset:
				case NotifyCollectionChangedAction.Remove:
					foreach (var newObj in e.OldItems)
						removeReference(newObj);
					break;
				case NotifyCollectionChangedAction.Replace:
					foreach (var newObj in e.NewItems)
						addReference(newObj);
					foreach (var newObj in e.OldItems)
						removeReference(newObj);
					break;
				case NotifyCollectionChangedAction.Move:
					break;
				default:
					throw new InvalidOperationException();
			}
		}

	}

	private static IEnumerable<object> getEntireObjectTree(object root)
	{
		var result = new HashSet<object>(ReferenceEqualityComparer.Instance);
		int count = root.TransitiveVirtualSelect<object, INotifyPropertyChanged, INotifyCollectionChanged>(getPropertyValuesOf, getCollectionValuesOf)
						.Count(); // used for side-effects
		return result;

		IEnumerable<object> getPropertyValuesOf(INotifyPropertyChanged obj)
		{
			Contract.Requires(obj != null);
			if (result.Contains(obj))
			{
				return Enumerable.Empty<object>();
			}
			else
			{
				result.Add(obj);
			}

			var properties = obj.GetType()
								.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
								.Where(prop => prop.GetIndexParameters().Length == 0)
								.Select(prop => prop.GetValue(obj))
								.EnsureSingleEnumerationDEBUG();
			return properties;
		}
		IEnumerable<object> getCollectionValuesOf(INotifyCollectionChanged collection)
		{
			Contract.Requires(collection != null);
			if (result.Contains(collection))
			{
				return Enumerable.Empty<object>();
			}
			else
			{
				result.Add(collection);
			}

			if (!typeof(IEnumerable<>).IsAssignableFrom(collection.GetType()))
			{
				throw new NotImplementedException();
			}

			return (IEnumerable<object>)collection;
		}
	}
}
internal class BindRecursivelyDisposable : Disposable
{
	public IReadOnlyDictionary<object, int> Data { get; }
	public BindRecursivelyDisposable(Dictionary<object, int> allHookedObservables, Action dispose)
		: base(dispose)
	{
		this.Data = new ReadOnlyDictionary<object, int>(allHookedObservables);
	}

}
