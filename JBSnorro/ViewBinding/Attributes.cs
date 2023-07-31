#nullable disable
using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace JBSnorro.View.Binding;

/// <summary>
/// Indicates the that associated property is not (directly) part of the interface to be sent to the view.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class NoViewBindingAttribute : Attribute
{

}


public interface IExtraViewPropertiesContainer
{
	IReadOnlyDictionary<string, object> Properties { get; }
}
public abstract class ViewBindingAttribute : Attribute
{
	/// <summary>
	/// Gets an object that represents the actual (serializable) view model of the specified object.
	/// </summary>
	public abstract object GetSubstitute(object obj);
}

public abstract class CachedViewBindingAsAttribute : ViewBindingAttribute
{
	/// <summary>
	/// This is a global dictionary that maps all objects that have ever been substituted by another object to that object.
	/// </summary>
	internal static readonly WeakReferenceDictionary<object, object> mappedObjects = new WeakReferenceDictionary<object, object>(ReferenceEqualityComparer.Instance);

	public override object GetSubstitute(object obj) => GetOrCreateSubstitute(obj);
	/// <summary>
	/// Gets an object that represents the actual (serializable) view model of the specified object.
	/// </summary>
	public object GetOrCreateSubstitute(object obj)
	{
		Contract.Requires(obj != null, "Only non-null properties can be substituted");
		// Contract.Requires(obj.GetType().IsClass, "Only reference types can be substituted");

		if (!mappedObjects.TryGetValue(obj, out object substitute))
		{
			substitute = this.createSubstitute(obj);
			mappedObjects.Add(obj, substitute);
		}
		return substitute;
	}
	/// <summary>
	/// Creates an object that represents the actual (serializable) view model of the specified object, assuming the specified object does not already have a substitute.
	/// </summary>
	protected abstract object createSubstitute(object obj);
}
/// <summary>
/// Indicates that the associated property is to be mapped onto a typescript Map object.
/// </summary>
public abstract class ViewBindingAsMapAttribute : CachedViewBindingAsAttribute
{
	/// <summary>
	/// Gets the name of the typescript property under which the object is to be assigned.
	/// From the perspective that we're mapping a collection to a Dictionary&lt;string,object&gt;, this function gets the key.
	/// </summary>
	/// <param name="value"> The object for which we're returning the key. </param>
	/// <param name="index"> The index in the collection for which we're returning the key. </param>
	protected abstract string GetAttributeName(object value, int index);

	/// <summary>
	/// Creates an object that represents the actual view model of the specified object, assuming the specified object does not already have a substitute.
	/// </summary>
	protected sealed override object createSubstitute(object collection) => createSubstitute((INotifyCollectionChanged)collection);
	public INotifyPropertyChanged createSubstitute(INotifyCollectionChanged collection)
	{
		var result = new Substitute();
		collection.CollectionChanged += onCollectionChange;

		if (collection is IEnumerable enumerable)
		{
			var currentCollection = enumerable.Cast<object>().ToList();
			onCollectionChange(collection, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, currentCollection));
		}
		else
		{
			System.Diagnostics.Debug.WriteLine($"A collection of type '{collection.GetType()}' does not derive from 'IEnumerable' and hence no initial elements were detectable");
		}
		return result;
		void onCollectionChange(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					int newIndex = e.NewStartingIndex;
					foreach (object newItem in e.NewItems)
					{
						string name = this.GetAttributeName(newItem, newIndex);
						Contract.Assert(!string.IsNullOrEmpty(name), "The attribute name may not be null or empty");
						Contract.Assert(!result.Properties.ContainsKey(name), $"The collection already contains a member with name '${name}'");
						result.Properties[name] = newItem;
						result.Invoke(result, PropertyMutatedEventArgsExtensions.Create(name, typeof(object), null, newItem));
						newIndex++;
					}
					break;
				case NotifyCollectionChangedAction.Move:
					break; // don't do anything
				case NotifyCollectionChangedAction.Remove:
					int oldIndex = e.OldStartingIndex;
					foreach (object newItem in e.OldItems)
					{
						string name = this.GetAttributeName(newItem, oldIndex);
						Contract.Assert(!string.IsNullOrEmpty(name), "The attribute name may not be null or empty");
						Contract.Assert(result.Properties.ContainsKey(name), $"The collection did not contain a member with name '${name}' to remove");
						var oldItem = result.Properties[name];
						result.Properties[name] = null;
						result.Invoke(result, PropertyMutatedEventArgsExtensions.Create(name, typeof(object), oldItem, null));
						oldIndex++;
					}
					break;
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Reset:
					throw new NotImplementedException();
				default:
					throw new ArgumentException();
			}
		}
	}
	private sealed class Substitute : INotifyPropertyChanged, IExtraViewPropertiesContainer
	{
		[NoViewBinding]
		public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

		IReadOnlyDictionary<string, object> IExtraViewPropertiesContainer.Properties => Properties;

		public event PropertyChangedEventHandler PropertyChanged;
		internal void Invoke(object sender, PropertyChangedEventArgs e)
			=> this.PropertyChanged?.Invoke(sender, e);
	}
}
/// <summary>
/// Specifies that the string property this attribute is attached to contains an identifier.
/// This implies that it will be serialized accordingly (i.e. by default, the first letter is lowercased).
/// </summary>
public class IdentifierViewBindingAttribute : ViewBindingAttribute
{
	public static string ToTypescriptIdentifier(string s) => s.ToFirstLower();
	public static string ToCSharpIdentifier(string s) => s.ToFirstUpper();

	public override object GetSubstitute(object obj)
	{
		switch (obj)
		{
			case null:
				return null;
			case string s:
				return ToTypescriptIdentifier(s);
			default:
				throw new ArgumentException($"The attribute '${nameof(IdentifierViewBindingAttribute)}' can only be applied to string properties");
		}
	}
}
/// <summary>
/// Specifies a different name than the default name, which is derived from the property name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class ViewBindingDisplayAttribute : Attribute
{
	public string SerializedName { get; }
	public ViewBindingDisplayAttribute(string serializedName)
	{
		this.SerializedName = serializedName;
	}
}
