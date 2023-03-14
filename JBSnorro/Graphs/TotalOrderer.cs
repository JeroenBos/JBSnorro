#nullable enable
using JBSnorro.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JBSnorro.Extensions;
using JBSnorro.Algorithms;
using JBSnorro;

namespace JBSnorro.Graphs;

/// <summary>
/// Threadsafe. Just keep adding links, <see cref="CircularDependencyException"/> will be thrown if there's a circular dependency.
/// Otherwise a total order can be established.
/// This does not support elements that are considered equal. 
/// The strategy is to add all inequal pairs (a, b) where a > b to <see cref="TotalOrderer{T}.Add"/> and <see cref="TotalOrderer{T}.GetTotalOrder"/> returns an increasing sequence.
/// </summary>
public class TotalOrderer<T> : CircularDependencyTracker<T> where T : notnull
{
    public TotalOrderer(IEqualityComparer<T>? equalityComparer = null) : base(equalityComparer)
    {
    }

    public IEnumerable<T> GetTotalOrder()
    {
        return GetTotalOrder(base.Dependencies, base.EqualityComparer);
    }
    private static IEnumerable<T> GetTotalOrder(ImmutableDictionary<T, ImmutableHashSet<T>> relationships, IEqualityComparer<T> equalityComparer)
    {
        if (relationships.Count == 1)
        {
            return relationships.Keys;
        }

        // find a node that's not anybody's dependency
        var thoseNotDependedOn = new HashSet<T>(relationships.Keys, equalityComparer);
        foreach (var relationship in relationships)
        {
            foreach (var dependency in relationship.Value)
            {
                thoseNotDependedOn.Remove(dependency);
            }
        }

        Contract.Assert(thoseNotDependedOn.Count != 0, "Can't be, because a circular dependency exception should have been thrown");

        var remainingTree = relationships.RemoveRange(thoseNotDependedOn);
        var remainingTotalOrder = GetTotalOrder(remainingTree, equalityComparer);
        return remainingTotalOrder.Concat(thoseNotDependedOn);
    }
}
