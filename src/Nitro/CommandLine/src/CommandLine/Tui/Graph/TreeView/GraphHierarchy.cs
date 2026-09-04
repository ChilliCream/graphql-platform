namespace ChilliCream.Nitro.CommandLine.Tui.Graph.TreeView;

/// <summary>
/// Builds the parent-child hierarchy used by the graph tree projection.
/// Nodes without a visible parent are children of the virtual root.
/// </summary>
internal static class GraphHierarchy
{
    /// <summary>
    /// Creates a hierarchy over the graph's parent-child edges. Each node has
    /// at most one parent and cycles are detached at a deterministic root.
    /// </summary>
    public static GraphHierarchyNode Build(GraphModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var nodesById = model.Nodes.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var parentsByChild = GraphParentMap.Build(model);

        var childrenByParent = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var (childId, parentId) in parentsByChild)
        {
            if (!childrenByParent.TryGetValue(parentId, out var childIds))
            {
                childIds = [];
                childrenByParent[parentId] = childIds;
            }

            childIds.Add(childId);
        }

        foreach (var childIds in childrenByParent.Values)
        {
            childIds.Sort((left, right) => Compare(nodesById[left], nodesById[right]));
        }

        var rootIds = nodesById.Keys
            .Where(id => !parentsByChild.ContainsKey(id))
            .OrderBy(id => nodesById[id].Priority)
            .ThenBy(id => id, StringComparer.Ordinal)
            .ToArray();

        return new GraphHierarchyNode(null, rootIds.Select(BuildNode).ToArray());

        GraphHierarchyNode BuildNode(string id)
        {
            var childIds = childrenByParent.GetValueOrDefault(id) ?? [];
            return new GraphHierarchyNode(nodesById[id], childIds.Select(BuildNode).ToArray());
        }
    }

    private static int Compare(GraphNode left, GraphNode right)
    {
        var priority = left.Priority.CompareTo(right.Priority);
        return priority != 0 ? priority : StringComparer.Ordinal.Compare(left.Id, right.Id);
    }
}
