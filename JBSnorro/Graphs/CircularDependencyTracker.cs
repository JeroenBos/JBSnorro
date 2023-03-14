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
/// </summary>
public class CircularDependencyTracker<T> where T : notnull
{
    private ImmutableDictionary<T, ImmutableHashSet<T>> dependencies;
    private readonly IEqualityComparer<T> comparer;

    public CircularDependencyTracker(IEqualityComparer<T>? equalityComparer = null)
    {
        this.comparer = equalityComparer ?? EqualityComparer<T>.Default;
        this.dependencies = ImmutableDictionary.Create<T, ImmutableHashSet<T>>(this.comparer);
    }


    /// <summary>
    /// Gets whether the specified node has the specified dependency directly.
    /// </summary>
    public bool ContainsDirecty(T node, T dependency)
    {
        if (!dependencies.TryGetValue(node, out var result))
        {
            return false;
        }

        return result.Contains(dependency);
    }
    /// <summary>
    /// Gets whether the specified node depends on the dependency, either directly or indirectly.
    /// </summary>
    public bool Contains(T node, T dependency)
    {
        return Contains(new[] { node }, dependency);
    }
    /// <summary>
    /// Gets whether any of the specified nodes depends on the dependency, either directly or indirectly.
    /// </summary>
    public bool Contains(IEnumerable<T> nodes, T dependency)
    {
        if (EnumerableExtensions.IsEmpty(ref nodes))
        {
            return false;
        }

        var visited = new HashSet<T>(new[] { dependency }, this.comparer);
        var path = Dijkstra<T>.FindPath(nodes, getLinks, IsTarget);
        return path != null;

        IEnumerable<T> getLinks(T key)
        {
            if (this.dependencies.TryGetValue(key, out var result))
            {
                return result;
            }
            return Array.Empty<T>();
        }
        bool IsTarget(T key)
        {
            if (visited.Contains(key))
            {
                return true;
            }
            visited.Add(key);
            return false;
        }
    }
    /// <summary>
    /// Adds the specified node with its dependencies, throwing if it causes circular dependencies.
    /// </summary>
    public void Add(T node, params T[] dependencies)
    {
        Add(node, (IReadOnlyCollection<T>)dependencies);
    }
    /// <summary>
    /// Adds the specified node with its dependencies, throwing if it causes circular dependencies.
    /// </summary>
    public void Add(T node, IEnumerable<T> dependencies)
    {
        Add(node, (IReadOnlyCollection<T>)dependencies.ToArray());
    }
    /// <summary>
    /// Adds the specified node with its dependencies, throwing if it causes circular dependencies.
    /// </summary>
    public void Add(T node, IReadOnlyCollection<T> dependencies)
    {
        if (dependencies.Contains(node))
        {
            throw new CircularDependencyException($"The following node depends on itself: '{node}'");
        }
        ImmutableInterlocked.AddOrUpdate(ref this.dependencies, node, addValueFactory, updateValueFactory);



        ImmutableHashSet<T> addValueFactory(T key)
        {
            if (this.Contains(dependencies, key))
            {
                throw CircularDependencyException(key, dependencies);
            }
            return ImmutableHashSet.CreateRange(this.comparer, dependencies);
        }

        ImmutableHashSet<T> updateValueFactory(T key, ImmutableHashSet<T> currentValue)
        {
            int currentCount = currentValue.Count;
            var result = currentValue.Union(dependencies);
            if (currentCount + dependencies.Count != result.Count)
            {
                throw DependencyAlreadyExistsException(key, currentValue, result);
            }
            if (this.Contains(dependencies, key))
            {
                throw CircularDependencyException(key, dependencies);
            }

            return result;
        }
    }

    private CircularDependencyException DependencyAlreadyExistsException(T key, ImmutableHashSet<T> current, IReadOnlyCollection<T> newDependencies)
    {
        var alreadyExisting = current.Intersect(newDependencies);
        Contract.Assert(alreadyExisting.Count != 0);

        if (alreadyExisting.Count == 1)
        {
            return new CircularDependencyException($"The following dependency already exists: '{key}'->'{alreadyExisting.First()}'");
        }
        else
        {
            string message = alreadyExisting.Select(alreadyExisting => $"    '{key}'->'{alreadyExisting}'")
                                            .Join("\n");
            return new CircularDependencyException($"The following dependencies already exist: \n" + message);
        }
    }
    private CircularDependencyException CircularDependencyException(T key, IReadOnlyCollection<T> newDependencies)
    {
        if (newDependencies.Count == 1)
        {
            return new CircularDependencyException($"'{key}' is already depended on (indirectly) by {newDependencies.First()}");
        }
        else
        {
            string message = newDependencies.Select(newDependency => $"    - '{newDependency}'")
                                            .Join("\n");
            return new CircularDependencyException($"'{key}' is already depended on (indirectly) by any of \n{message}");
        }

    }
}

