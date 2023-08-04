using JBSnorro.Collections;
using JBSnorro.Diagnostics;

namespace JBSnorro.Algorithms;

public static class Dijkstra<T> where T : notnull
{
	internal struct Node : IComparable<Node>
	{
		public T Element { get; }
		public Option<T> From { get; }
		public int DistanceFromInitialElements { get; }

		public Node(T initialElement)
		{
			Element = initialElement;
			From = default;
			DistanceFromInitialElements = 0;
		}
		public Node(T element, Node from)
		{
			Element = element;
			From = from.Element;
			DistanceFromInitialElements = from.DistanceFromInitialElements + 1;
		}

		public int CompareTo(Node obj)
		{
			return this.DistanceFromInitialElements.CompareTo(obj.DistanceFromInitialElements);
		}

		public override int GetHashCode()
		{
			return Element.GetHashCode();
		}
		public override bool Equals(object? obj)
		{
			if (obj is Node node)
			{
				// Use default equality comparer for T
				return Element.Equals(node.Element);
			}
			return false;
		}
	}
	public static IEnumerable<T>? FindPath(IEnumerable<T> initialElements, Func<T, IEnumerable<T>> getLinkedNodes, Func<T, bool> isTarget, IEqualityComparer<T>? equalityComparer = null)
	{
		return FindPath(initialElements, getLinkedNodes, (t, _) => isTarget(t), equalityComparer);
	}
	public static IEnumerable<T>? FindPath(IEnumerable<T> initialElements, Func<T, IEnumerable<T>> getLinkedNodes, Func<T, int/*distance from any initial element*/, bool> isTarget, IEqualityComparer<T>? equalityComparer = null)
	{
		equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;

		Contract.Requires(initialElements != null);
		Contract.LazilyAssertMinimumCount(ref initialElements, 1);
		Contract.Requires(getLinkedNodes != null);
		Contract.Requires(isTarget != null);

		// the keys function as hash set, allowing for quick checks whether an element is already used. The value per key is the element from which the resulting path came, to allow for backtracking
		var closed = new Dictionary<T, Option<T>>(equalityComparer);

		var open = new Heap<Node>(Enumerable.Empty<Node>());
		foreach (T initialElement in initialElements)
		{
			if (isTarget(initialElement, 0))
			{
				return initialElement.ToSingleton();
			}
			else
			{
				open.Add(new Node(initialElement));
			}
		}


		while (open.Count != 0)
		{
			Node element = open.RemoveNext();//first in order of DistanceFromAnyInitialElements
			closed.Add(element.Element, element.From);

			foreach (T connectedNode in getLinkedNodes(element.Element))
			{
				var newNode = new Node(connectedNode, element);
				if (isTarget(newNode.Element, newNode.DistanceFromInitialElements))
				{
					closed.Add(newNode.Element, element.Element);
					return Path(newNode, closed);
				}
				else if (!closed.ContainsKey(connectedNode) && !open.Contains(newNode))
				{
					open.Add(newNode);
				}
			}
		}

		//no path was found
		return null;
	}

	private static IEnumerable<T> Path(Node target, Dictionary<T, Option<T>> backtracker)
	{
		Option<T> pathElement = target.Element;
		var result = new T[target.DistanceFromInitialElements + 1];
		for (int i = result.Length - 1; i >= 0; i--)
		{
			result[i] = pathElement.Value;
			pathElement = backtracker[pathElement.Value];
		}
		return result;
	}
}
