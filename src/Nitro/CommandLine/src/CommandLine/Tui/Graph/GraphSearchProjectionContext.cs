namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Resolves filtered graph matches to their current canvas representatives.
/// </summary>
internal sealed class GraphSearchProjectionContext
{
    private readonly IReadOnlyDictionary<string, string> _parentByChild;
    private readonly HashSet<string> _reducedIds;
    private readonly Dictionary<string, string> _representatives = new(StringComparer.Ordinal);

    public GraphSearchProjectionContext(GraphModel visibleModel, GraphModel reducedModel)
    {
        VisibleNodes = visibleModel.Nodes;
        _parentByChild = GraphParentMap.Build(visibleModel);
        _reducedIds = reducedModel.Nodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Nodes that can directly match the active graph search, in stable order.
    /// </summary>
    public IReadOnlyList<GraphNode> VisibleNodes { get; }

    /// <summary>
    /// Resolves a visible task to its reduced canvas representative.
    /// </summary>
    public string ResolveRepresentative(string id)
    {
        if (_representatives.TryGetValue(id, out var representative))
        {
            return representative;
        }

        var current = id;
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (true)
        {
            if (_reducedIds.Contains(current))
            {
                representative = current;
                break;
            }

            if (!visited.Add(current) || !_parentByChild.TryGetValue(current, out var parentId))
            {
                representative = id;
                break;
            }

            current = parentId;
        }

        _representatives[id] = representative;
        return representative;
    }

    /// <summary>
    /// Whether the id is represented in the current reduced canvas model.
    /// </summary>
    public bool ContainsReducedId(string id) => _reducedIds.Contains(id);
}
