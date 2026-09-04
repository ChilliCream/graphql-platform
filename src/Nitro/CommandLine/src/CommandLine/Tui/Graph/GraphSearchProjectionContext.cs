namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Resolves filtered graph matches to their current canvas representatives.
/// </summary>
internal sealed class GraphSearchProjectionContext
{
    private readonly HashSet<string> _reducedIds;
    private readonly Dictionary<string, string> _representatives = new(StringComparer.Ordinal);

    public GraphSearchProjectionContext(GraphModel visibleModel, GraphModel reducedModel)
    {
        VisibleNodes = visibleModel.Nodes;
        _reducedIds = reducedModel.Nodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        BuildRepresentatives(GraphParentMap.Build(visibleModel));
    }

    /// <summary>
    /// Nodes that can directly match the active graph search, in stable order.
    /// </summary>
    public IReadOnlyList<GraphNode> VisibleNodes { get; }

    /// <summary>
    /// Number of nodes visited while constructing the representative table.
    /// </summary>
    internal int RepresentativeBuildVisitCount { get; private set; }

    /// <summary>
    /// Resolves a visible task to its reduced canvas representative.
    /// </summary>
    public string ResolveRepresentative(string id)
        => _representatives.GetValueOrDefault(id, id);

    /// <summary>
    /// Whether the id is represented in the current reduced canvas model.
    /// </summary>
    public bool ContainsReducedId(string id) => _reducedIds.Contains(id);

    private void BuildRepresentatives(IReadOnlyDictionary<string, string> parentByChild)
    {
        var childrenByParent = parentByChild
            .GroupBy(t => t.Value, StringComparer.Ordinal)
            .ToDictionary(
                t => t.Key,
                t => t.Select(child => child.Key).Order(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
        var childIds = parentByChild.Keys.ToHashSet(StringComparer.Ordinal);
        var pending = new Queue<(string Id, string? NearestReducedId)>();

        foreach (var node in VisibleNodes)
        {
            if (!childIds.Contains(node.Id))
            {
                pending.Enqueue((node.Id, null));
            }
        }

        while (pending.TryDequeue(out var current))
        {
            RepresentativeBuildVisitCount++;
            var nearestReducedId = _reducedIds.Contains(current.Id)
                ? current.Id
                : current.NearestReducedId;
            _representatives[current.Id] = nearestReducedId ?? current.Id;

            if (childrenByParent.TryGetValue(current.Id, out var childIdsForParent))
            {
                foreach (var childId in childIdsForParent)
                {
                    pending.Enqueue((childId, nearestReducedId));
                }
            }
        }

        foreach (var node in VisibleNodes)
        {
            _representatives.TryAdd(node.Id, node.Id);
        }
    }
}
