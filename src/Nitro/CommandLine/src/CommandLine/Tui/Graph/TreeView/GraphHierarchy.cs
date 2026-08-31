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
        var parentsByChild = BuildParents(model.Edges, nodesById);
        BreakCycles(parentsByChild, nodesById);

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

    private static Dictionary<string, string> BuildParents(
        IReadOnlyList<GraphEdge> edges,
        IReadOnlyDictionary<string, GraphNode> nodesById)
    {
        var candidatesByChild = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var edge in edges)
        {
            if (edge.Kind != GraphEdgeKind.ParentChild
                || edge.FromId == edge.ToId
                || !nodesById.ContainsKey(edge.FromId)
                || !nodesById.ContainsKey(edge.ToId))
            {
                continue;
            }

            if (!candidatesByChild.TryGetValue(edge.ToId, out var parentIds))
            {
                parentIds = new HashSet<string>(StringComparer.Ordinal);
                candidatesByChild[edge.ToId] = parentIds;
            }

            parentIds.Add(edge.FromId);
        }

        var parentsByChild = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (childId, parentIds) in candidatesByChild)
        {
            parentsByChild[childId] = parentIds
                .OrderBy(id => nodesById[id].Priority)
                .ThenBy(id => id, StringComparer.Ordinal)
                .First();
        }

        return parentsByChild;
    }

    private static void BreakCycles(
        Dictionary<string, string> parentsByChild,
        IReadOnlyDictionary<string, GraphNode> nodesById)
    {
        foreach (var startId in nodesById.Keys.OrderBy(id => nodesById[id].Priority).ThenBy(id => id, StringComparer.Ordinal))
        {
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            var path = new List<string>();
            var currentId = startId;

            while (true)
            {
                if (positions.TryGetValue(currentId, out var cycleStart))
                {
                    var rootId = path
                        .Skip(cycleStart)
                        .OrderBy(id => nodesById[id].Priority)
                        .ThenBy(id => id, StringComparer.Ordinal)
                        .First();
                    parentsByChild.Remove(rootId);
                    break;
                }

                positions[currentId] = path.Count;
                path.Add(currentId);

                if (!parentsByChild.TryGetValue(currentId, out var parentId))
                {
                    break;
                }

                currentId = parentId;
            }
        }
    }

    private static int Compare(GraphNode left, GraphNode right)
    {
        var priority = left.Priority.CompareTo(right.Priority);
        return priority != 0 ? priority : StringComparer.Ordinal.Compare(left.Id, right.Id);
    }
}

/// <summary>
/// A node in the graph tree hierarchy. The root has no task and contains all
/// visible top-level nodes.
/// </summary>
internal sealed record GraphHierarchyNode(GraphNode? Task, IReadOnlyList<GraphHierarchyNode> Children)
{
    /// <summary>
    /// Whether this is the hierarchy's virtual root node.
    /// </summary>
    public bool IsRoot => Task is null;
}
