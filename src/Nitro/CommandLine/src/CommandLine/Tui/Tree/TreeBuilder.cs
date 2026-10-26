using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Tree;

/// <summary>
/// Expands a task's dependency graph into a tree rooted at a given task, and
/// flattens it into <see cref="TreeNodeRow"/> for display.
/// </summary>
internal static class TreeBuilder
{
    /// <summary>
    /// The traversal depth used when no explicit depth is requested.
    /// </summary>
    public const int DefaultMaxDepth = 3;

    /// <summary>
    /// The deepest a traversal is allowed to go, regardless of the requested
    /// depth.
    /// </summary>
    public const int HardMaxDepth = 10;

    /// <summary>
    /// Builds the flattened row list for the tree rooted at
    /// <paramref name="rootId"/> over <paramref name="edges"/>, following
    /// only edges that match <paramref name="edgeMode"/> in
    /// <paramref name="direction"/>. Traversal is breadth-first, so a task
    /// reachable by more than one path is expanded once, at the shallowest
    /// depth it is reached from; every later occurrence, including one that
    /// cycles back to an ancestor, renders as an unexpanded row with
    /// <see cref="TreeNodeRow.IsCycle"/> set. <paramref name="maxDepth"/> is
    /// clamped to <see cref="HardMaxDepth"/>.
    /// </summary>
    public static IReadOnlyList<TreeNodeRow> Build(
        string rootId,
        IReadOnlyList<TaskDependency> edges,
        TreeEdgeMode edgeMode,
        TreeDirection direction,
        int maxDepth = DefaultMaxDepth)
    {
        ArgumentNullException.ThrowIfNull(rootId);
        ArgumentNullException.ThrowIfNull(edges);

        var cappedDepth = Math.Clamp(maxDepth, 0, HardMaxDepth);
        var adjacency = BuildAdjacency(edges, edgeMode, direction);

        // Breadth-first expansion: every node's first-seen depth is its
        // shortest distance from the root, and its children are only those
        // discovered while it is dequeued.
        var depthById = new Dictionary<string, int> { [rootId] = 0 };
        var childrenByParent = new Dictionary<string, List<(string ChildId, bool IsCycle)>>();
        var queue = new Queue<string>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var currentDepth = depthById[currentId];

            if (currentDepth >= cappedDepth || !adjacency.TryGetValue(currentId, out var children))
            {
                continue;
            }

            var childList = new List<(string, bool)>(children.Count);

            foreach (var childId in children)
            {
                if (depthById.ContainsKey(childId))
                {
                    childList.Add((childId, true));
                    continue;
                }

                depthById[childId] = currentDepth + 1;
                childList.Add((childId, false));
                queue.Enqueue(childId);
            }

            childrenByParent[currentId] = childList;
        }

        // Pre-order flatten so each row's connector only needs the sibling
        // position of its own ancestors, already resolved on the way down.
        var rows = new List<TreeNodeRow>();
        Flatten(rootId, depth: 0, isLastChild: true, ancestorIsLastChild: [], childrenByParent, rows);
        return rows;
    }

    private static void Flatten(
        string nodeId,
        int depth,
        bool isLastChild,
        IReadOnlyList<bool> ancestorIsLastChild,
        IReadOnlyDictionary<string, List<(string ChildId, bool IsCycle)>> childrenByParent,
        List<TreeNodeRow> rows)
    {
        rows.Add(new TreeNodeRow
        {
            TaskId = nodeId,
            Depth = depth,
            IsLastChild = isLastChild,
            AncestorIsLastChild = ancestorIsLastChild,
            IsCycle = false
        });

        if (!childrenByParent.TryGetValue(nodeId, out var children) || children.Count == 0)
        {
            return;
        }

        var childAncestors = depth == 0 ? [] : Append(ancestorIsLastChild, isLastChild);

        for (var i = 0; i < children.Count; i++)
        {
            var (childId, isCycle) = children[i];
            var childIsLast = i == children.Count - 1;

            if (isCycle)
            {
                rows.Add(new TreeNodeRow
                {
                    TaskId = childId,
                    Depth = depth + 1,
                    IsLastChild = childIsLast,
                    AncestorIsLastChild = childAncestors,
                    IsCycle = true
                });
                continue;
            }

            Flatten(childId, depth + 1, childIsLast, childAncestors, childrenByParent, rows);
        }
    }

    private static IReadOnlyList<bool> Append(IReadOnlyList<bool> list, bool value)
    {
        var next = new bool[list.Count + 1];

        for (var i = 0; i < list.Count; i++)
        {
            next[i] = list[i];
        }

        next[^1] = value;
        return next;
    }

    private static Dictionary<string, List<string>> BuildAdjacency(
        IReadOnlyList<TaskDependency> edges,
        TreeEdgeMode edgeMode,
        TreeDirection direction)
    {
        var adjacency = new Dictionary<string, List<string>>();

        foreach (var edge in edges)
        {
            if (!Matches(edge.Type, edgeMode))
            {
                continue;
            }

            // TaskId depends on DependsOnId. Up walks toward what a node
            // depends on (node -> DependsOnId); down walks toward what
            // depends on a node, following the same edge backward
            // (node -> TaskId).
            var (from, to) = direction == TreeDirection.Up
                ? (edge.TaskId, edge.DependsOnId)
                : (edge.DependsOnId, edge.TaskId);

            if (!adjacency.TryGetValue(from, out var children))
            {
                children = [];
                adjacency[from] = children;
            }

            children.Add(to);
        }

        foreach (var children in adjacency.Values)
        {
            children.Sort(StringComparer.Ordinal);
        }

        return adjacency;
    }

    private static bool Matches(string type, TreeEdgeMode edgeMode) => edgeMode switch
    {
        TreeEdgeMode.ParentChild => type == TaskDependencyTypes.ParentChild,
        _ => TaskDependencyTypes.IsBlocking(type)
    };
}
