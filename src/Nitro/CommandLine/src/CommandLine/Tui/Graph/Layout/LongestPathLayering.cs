namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// Assigns layers by the longest directed path from a source vertex.
/// </summary>
internal sealed class LongestPathLayering : IGraphLayeringStrategy
{
    public IReadOnlyDictionary<string, int> AssignLayers(
        IReadOnlyList<string> nodeIds,
        IReadOnlyList<LayoutArc> arcs,
        GraphLayoutResult? previousLayout)
    {
        var indegrees = nodeIds.ToDictionary(t => t, _ => 0, StringComparer.Ordinal);
        var outgoing = nodeIds.ToDictionary(t => t, _ => new List<LayoutArc>(), StringComparer.Ordinal);

        foreach (var arc in arcs)
        {
            indegrees[arc.ToId]++;
            outgoing[arc.FromId].Add(arc);
        }

        var previousLayers = previousLayout?.Nodes.ToDictionary(t => t.Id, t => t.Layer, StringComparer.Ordinal)
            ?? new Dictionary<string, int>(StringComparer.Ordinal);
        var ready = new SortedSet<LayerCandidate>(LayerCandidateComparer.Instance);
        foreach (var nodeId in nodeIds)
        {
            if (indegrees[nodeId] == 0)
            {
                ready.Add(new LayerCandidate(nodeId, previousLayers.GetValueOrDefault(nodeId)));
            }
        }

        var layers = nodeIds.ToDictionary(t => t, _ => 0, StringComparer.Ordinal);
        while (ready.Count > 0)
        {
            var candidate = ready.Min!;
            ready.Remove(candidate);

            foreach (var arc in outgoing[candidate.Id])
            {
                layers[arc.ToId] = Math.Max(layers[arc.ToId], layers[candidate.Id] + 1);
                if (--indegrees[arc.ToId] == 0)
                {
                    ready.Add(new LayerCandidate(arc.ToId, previousLayers.GetValueOrDefault(arc.ToId)));
                }
            }
        }

        return layers;
    }

    private readonly record struct LayerCandidate(string Id, int PreviousLayer);

    private sealed class LayerCandidateComparer : IComparer<LayerCandidate>
    {
        public static LayerCandidateComparer Instance { get; } = new();

        public int Compare(LayerCandidate x, LayerCandidate y)
        {
            var byLayer = x.PreviousLayer.CompareTo(y.PreviousLayer);
            return byLayer != 0 ? byLayer : string.CompareOrdinal(x.Id, y.Id);
        }
    }
}
