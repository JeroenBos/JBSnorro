using JBSnorro.Diagnostics;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JBSnorro;

public abstract class PersistentTreeBaseNode<T> : PersistentTreeBase<T>
{
	/// <summary> Gets the value this node holds. </summary>
	public T Value
	{
		get { return base.value; }
		//set { base.value = value; }
	}

	public new PersistentTreeNode<T>? Parent
	{
		get { return (PersistentTreeNode<T>?)base.Parent; }
	}
	public PersistentTreeBaseNode(T value) : base(value)
	{
	}
}
/// <summary> This represents the base for the leaves and composite nodes of a persistent tree.  </summary>
public abstract class PersistentTreeBase<T>
{
	/// <summary> Gets the value this node holds. </summary>
	protected T value { get; set; }

	/// <summary> This singleton value is  </summary>
	private static readonly PersistentTreeLeaf<T> noParent = new PersistentTreeLeaf<T>(default!);
	/// <summary> This backingfield contains the parent of this node if it is set, or the noParent singleton when this represents a root node.
	/// It holds null when the parent hasn't been set yet. </summary>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private PersistentTreeBase<T>? parent;
	/// <summary> Gets the parent of this node (null if this is the root) if it is set. It can be set by calling ComputeParentalTree. </summary>
	public PersistentTreeBase<T>? Parent
	{
		get
		{
			Contract.Requires(this.ParentIsSet, "The parent hasn't been set yet, call " + nameof(PersistentTreeNode<T>.ComputeParentalTree));

			if (ReferenceEquals(parent, noParent))
				return null;
			return parent;
		}
	}
	/// <summary> Gets whether the parent is set, which is achieved by calling ComputeParentalTree. </summary>
	public bool ParentIsSet => parent != null;

    protected PersistentTreeBase(T value)
    {
        this.value = value;
    }

    /// <summary> Sets the parent of this persistent tree node. Throws if it is already set. Specify null to indicate this is the root. </summary>
    public virtual void SetParent(PersistentTreeBase<T>? parent)
	{
		Contract.Requires(this.parent == null);

		this.parent = parent ?? noParent;
	}

	public override string ToString()
	{
		if (ReferenceEquals(this, noParent))
			return "no parent";
		return base.ToString()!;
	}
}
public class PersistentTreeLeaf<T> : PersistentTreeBaseNode<T>
{
	public PersistentTreeLeaf(T value) : base(value)
	{
	}
}

public class PersistentTreeNode<T> : PersistentTreeBaseNode<T>
{
	/// <summary> Gets the children elements in this node. </summary>
	public ReadOnlyCollection<PersistentTreeBaseNode<T>> Elements { get; }

	public PersistentTreeNode(T value, ReadOnlyCollection<PersistentTreeBaseNode<T>> elements) : base(value)
	{
		//guarantee that elements doesn't change
		this.Elements = elements;
	}


	public PersistentTreeNode<T> With(T value)
	{
		return new PersistentTreeNode<T>(value, Elements);
	}


	public PersistentTreeNode<T> WithInserted(int index, T elementValue)
	{
		PersistentTreeLeaf<T> newElement = new PersistentTreeLeaf<T>(elementValue);
		ReadOnlyCollection<PersistentTreeBaseNode<T>> newElements = this.Elements.Take(index)
																				 .Concat(newElement)
																				 .Concat(this.Elements.Skip(index))
																				 .ToReadOnlyList(this.Elements.Count + 1);
		return new PersistentTreeNode<T>(this.Value, newElements);
	}
	public PersistentTreeNode<T> Without(int index)
	{
		ReadOnlyCollection<PersistentTreeBaseNode<T>> newElements = this.Elements.Take(index - 1)
																				 .Concat(this.Elements.Skip(index))
																				 .ToReadOnlyList(this.Elements.Count - 1);

		return new PersistentTreeNode<T>(this.Value, newElements);
	}

	/// <summary> Creates a new node where the element at the specified index is replaced by the specified node, and propagates it up the parental tree. </summary>
	internal PersistentTreeNode<T> Replace(int index, PersistentTreeBaseNode<T> node)
	{
		ReadOnlyCollection<PersistentTreeBaseNode<T>> newElements = this.Elements.Take(index - 1)
																				 .Concat(node)
																				 .Concat(this.Elements.Skip(index))
																				 .ToReadOnlyList(this.Elements.Count);
		return new PersistentTreeNode<T>(this.Value, newElements);
	}

	/// <summary> Sets the parents on each node in this tree, assuming this is called on the root node. </summary>
	internal void ComputeParentalTree()
	{
		this.SetParent(null);
	}
	public override void SetParent(PersistentTreeBase<T>? parent)
	{
		base.SetParent(parent);
		foreach (var child in Elements)
			child.SetParent(this);
	}
}
