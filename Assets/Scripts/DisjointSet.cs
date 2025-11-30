using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DisjointSet<T>
{
    private readonly Dictionary<T, T> parent = new Dictionary<T, T>();
    private readonly object _lock = new object();

    public T Find(T item)
    {
        lock (_lock)
        {
            if (!parent.ContainsKey(item))
                parent[item] = item;
            if (!EqualityComparer<T>.Default.Equals(parent[item], item))
                parent[item] = Find(parent[item]);
            return parent[item];
        }
    }

    public void Union(T a, T b)
    {
        lock (_lock)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (!EqualityComparer<T>.Default.Equals(rootA, rootB))
                parent[rootB] = rootA;
        }
    }

    /// <summary>
    /// Returns a deep snapshot of all groups, safe for enumeration.
    /// </summary>
    public List<List<T>> GroupsSnapshot()
    {
        List<T> keysSnapshot;
        lock (_lock)
        {
            keysSnapshot = parent.Keys.ToList();
        }
        return keysSnapshot
            .GroupBy(Find)
            .Select(g => g.ToList())
            .ToList();
    }
}

