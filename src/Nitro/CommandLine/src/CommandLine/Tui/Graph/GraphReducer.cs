using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Applies the graph's visibility, epic-collapsing, and cycle-marking stages
/// in the order required before layout.
/// </summary>
internal static class GraphReducer
{
    public static GraphModel Reduce(GraphModel model, GraphReductionOptions? options = null)
    {
        options ??= new GraphReductionOptions();

        var filtered = Filter(model, options);
        var collapsedEpicIds = options.CollapsedEpicIds
            ?? (filtered.Nodes.Count > GraphReductionOptions.AdaptiveCollapseThreshold
                ? filtered.Nodes.Where(t => t.IsEpic).Select(t => t.Id).ToHashSet(StringComparer.Ordinal)
                : []);
        var reduced = CollapseEpics(filtered, collapsedEpicIds);

        if (reduced.Nodes.Count <= GraphReductionOptions.VisibleNodeCap)
        {
            return MarkReversedEdges(reduced);
        }

        var forcedOptions = options with { HideClosed = true };
        var forceFiltered = Filter(model, forcedOptions);
        var forcedEpicIds = forceFiltered.Nodes
            .Where(t => t.IsEpic)
            .Select(t => t.Id)
            .ToHashSet(StringComparer.Ordinal);
        var forced = CollapseEpics(forceFiltered, forcedEpicIds);
        var hiddenNodeCount = Math.Max(0, filtered.Nodes.Count - forced.Nodes.Count);

        return MarkReversedEdges(forced) with
        {
            IsReduced = true,
            HiddenNodeCount = hiddenNodeCount
        };
    }

    internal static GraphModel Order(GraphModel model)
    {
        var nodes = model.Nodes
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Id, StringComparer.Ordinal)
            .ToArray();
        var priorities = nodes.ToDictionary(t => t.Id, t => t.Priority, StringComparer.Ordinal);
        var edges = model.Edges
            .OrderBy(t => priorities[t.FromId])
            .ThenBy(t => t.FromId, StringComparer.Ordinal)
            .ThenBy(t => priorities[t.ToId])
            .ThenBy(t => t.ToId, StringComparer.Ordinal)
            .ThenBy(t => t.Kind)
            .ToArray();

        return model with { Nodes = nodes, Edges = edges };
    }

    /// <summary>
    /// Applies visibility and graph filters without collapsing epics or laying out nodes.
    /// </summary>
    internal static GraphModel Filter(GraphModel model, GraphReductionOptions options)
    {
        var nodes = model.Nodes.AsEnumerable();

        if (options.EpicIds is { Count: > 0 } epicIds)
        {
            var scopedIds = FindEpicDescendantIds(model, epicIds);
            nodes = nodes.Where(t => scopedIds.Contains(t.Id));
        }

        if (options.HideClosed)
        {
            nodes = nodes.Where(t => !TaskStates.IsTerminal(t.Status));
        }

        if (options.Labels is { Count: > 0 } labels)
        {
            nodes = nodes.Where(t => labels.All(label => t.Labels.Contains(label, StringComparer.Ordinal)));
        }

        var filteredNodes = nodes.ToArray();

        var nodeIds = filteredNodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var edges = model.Edges
            .Where(t => nodeIds.Contains(t.FromId) && nodeIds.Contains(t.ToId))
            .ToArray();

        return Order(new GraphModel(filteredNodes, edges));
    }

    private static HashSet<string> FindEpicDescendantIds(
        GraphModel model,
        IReadOnlySet<string> epicIds)
    {
        var nodeIds = model.Nodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var children = model.Edges
            .Where(t => t.Kind == GraphEdgeKind.ParentChild && nodeIds.Contains(t.FromId) && nodeIds.Contains(t.ToId))
            .GroupBy(t => t.FromId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(t => t.ToId).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var scopedIds = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>(epicIds.Where(nodeIds.Contains).OrderDescending(StringComparer.Ordinal));

        while (pending.TryPop(out var id))
        {
            if (!scopedIds.Add(id) || !children.TryGetValue(id, out var childIds))
            {
                continue;
            }

            for (var index = childIds.Length - 1; index >= 0; index--)
            {
                pending.Push(childIds[index]);
            }
        }

        return scopedIds;
    }

    private static GraphModel CollapseEpics(GraphModel model, IReadOnlySet<string> collapsedEpicIds)
    {
        if (collapsedEpicIds.Count == 0)
        {
            return Order(model);
        }

        var nodes = model.Nodes.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var parentByChild = GraphParentMap.Build(model);
        var activeEpicIds = collapsedEpicIds
            .Where(id => nodes.TryGetValue(id, out var node) && node.IsEpic)
            .ToHashSet(StringComparer.Ordinal);
        var representativeById = nodes.Keys.ToDictionary(id => id, id => Representative(id), StringComparer.Ordinal);
        var hiddenChildCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var pair in representativeById)
        {
            if (pair.Key != pair.Value)
            {
                hiddenChildCounts[pair.Value] = hiddenChildCounts.GetValueOrDefault(pair.Value) + 1;
            }
        }

        var reducedNodes = nodes.Values
            .Where(t => representativeById[t.Id] == t.Id)
            .Select(t => t with { HiddenChildCount = hiddenChildCounts.GetValueOrDefault(t.Id) })
            .ToArray();
        var seen = new HashSet<(string FromId, string ToId, GraphEdgeKind Kind)>();
        var reducedEdges = new List<GraphEdge>();

        foreach (var edge in model.Edges)
        {
            var fromId = representativeById[edge.FromId];
            var toId = representativeById[edge.ToId];

            if (fromId != toId && seen.Add((fromId, toId, edge.Kind)))
            {
                reducedEdges.Add(edge with { FromId = fromId, ToId = toId, IsReversed = false });
            }
        }

        return Order(new GraphModel(reducedNodes, reducedEdges));

        string Representative(string id)
        {
            var current = id;
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var representative = id;

            while (seenIds.Add(current)
                && parentByChild.TryGetValue(current, out var parentId))
            {
                current = parentId;

                if (activeEpicIds.Contains(current))
                {
                    representative = current;
                }
            }

            return representative;
        }
    }

    private static GraphModel MarkReversedEdges(GraphModel model)
    {
        var ordered = Order(model);
        var byFrom = ordered.Edges
            .Select((edge, index) => (edge, index))
            .GroupBy(t => t.edge.FromId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.index).Select(t => t.edge).ToArray(), StringComparer.Ordinal);
        var colors = ordered.Nodes.ToDictionary(t => t.Id, _ => 0, StringComparer.Ordinal);
        var reversed = new HashSet<GraphEdge>();

        foreach (var root in ordered.Nodes)
        {
            Visit(root.Id);
        }

        return Order(ordered with
        {
            Edges = ordered.Edges.Select(t => t with { IsReversed = reversed.Contains(t) }).ToArray()
        });

        void Visit(string id)
        {
            if (colors[id] != 0)
            {
                return;
            }

            colors[id] = 1;

            if (byFrom.TryGetValue(id, out var edges))
            {
                foreach (var edge in edges)
                {
                    if (colors[edge.ToId] == 1)
                    {
                        reversed.Add(edge);
                    }
                    else if (colors[edge.ToId] == 0)
                    {
                        Visit(edge.ToId);
                    }
                }
            }

            colors[id] = 2;
        }
    }
}
