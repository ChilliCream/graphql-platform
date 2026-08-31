using ChilliCream.Nitro.CommandLine.Tui.Graph;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// Makes graph edges acyclic for layout by reversing deterministic DFS back edges.
/// </summary>
internal static class GraphCycleGuard
{
    public static IReadOnlyList<LayoutArc> Guard(GraphModel model)
    {
        var nodeIds = model.Nodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var arcs = model.Edges
            .Where(t => nodeIds.Contains(t.FromId) && nodeIds.Contains(t.ToId) && t.FromId != t.ToId)
            .OrderBy(t => t.FromId, StringComparer.Ordinal)
            .ThenBy(t => t.ToId, StringComparer.Ordinal)
            .ThenBy(t => t.Kind)
            .Select(t => t.IsReversed
                ? new LayoutArc(t, t.ToId, t.FromId, true)
                : new LayoutArc(t, t.FromId, t.ToId, false))
            .ToArray();
        var outgoing = arcs
            .GroupBy(t => t.FromId, StringComparer.Ordinal)
            .ToDictionary(t => t.Key, t => t.ToList(), StringComparer.Ordinal);
        var colors = nodeIds.ToDictionary(t => t, _ => 0, StringComparer.Ordinal);

        foreach (var node in model.Nodes.OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            Visit(node.Id);
        }

        return arcs;

        void Visit(string id)
        {
            if (colors[id] != 0)
            {
                return;
            }

            colors[id] = 1;
            if (outgoing.TryGetValue(id, out var edges))
            {
                foreach (var arc in edges)
                {
                    if (colors[arc.ToId] == 1)
                    {
                        arc.FromId = arc.ToId;
                        arc.ToId = id;
                        arc.IsReversed = !arc.IsReversed;
                    }
                    else if (colors[arc.ToId] == 0)
                    {
                        Visit(arc.ToId);
                    }
                }
            }

            colors[id] = 2;
        }
    }
}
