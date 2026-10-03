using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

internal static class SpanTreeBuilder
{
    public static SpanTree Build(IReadOnlyList<TraceSpan> spans)
    {
        var nodes = new SpanTreeNode[spans.Count];
        var byId = new Dictionary<string, SpanTreeNode>(StringComparer.Ordinal);

        for (var i = 0; i < spans.Count; i++)
        {
            var node = new SpanTreeNode(spans[i]);
            nodes[i] = node;
            byId.TryAdd(node.Span.SpanId, node);
        }

        var roots = new List<SpanTreeNode>();

        foreach (var node in nodes)
        {
            var parentId = node.Span.ParentSpanId;
            if (string.IsNullOrEmpty(parentId)
                || !byId.TryGetValue(parentId, out var parent)
                || ReferenceEquals(parent, node))
            {
                roots.Add(node);
            }
            else
            {
                node.Parent = parent;
                parent.MutableChildren.Add(node);
            }
        }

        return new SpanTree(roots, nodes, byId);
    }
}
