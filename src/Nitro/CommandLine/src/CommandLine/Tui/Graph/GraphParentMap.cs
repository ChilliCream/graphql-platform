namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Builds the canonical parent for every node with a valid parent-child edge.
/// Self references and edges outside the model are ignored, and cyclic parent
/// chains are detached at their deterministic root.
/// </summary>
internal static class GraphParentMap
{
    /// <summary>
    /// Creates a child-to-parent map using parent priority and then ordinal id
    /// to select among multiple valid parents.
    /// </summary>
    public static Dictionary<string, string> Build(GraphModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var nodesById = model.Nodes.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var parentsByChild = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var edge in model.Edges)
        {
            if (edge.Kind != GraphEdgeKind.ParentChild
                || edge.FromId == edge.ToId
                || !nodesById.TryGetValue(edge.FromId, out var parent)
                || !nodesById.ContainsKey(edge.ToId))
            {
                continue;
            }

            if (!parentsByChild.TryGetValue(edge.ToId, out var existingParentId)
                || Compare(parent, nodesById[existingParentId]) < 0)
            {
                parentsByChild[edge.ToId] = edge.FromId;
            }
        }

        BreakCycles(parentsByChild, nodesById);
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
