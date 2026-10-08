namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

internal sealed class SpanTree(
    IReadOnlyList<SpanTreeNode> roots,
    IReadOnlyList<SpanTreeNode> nodes,
    IReadOnlyDictionary<string, SpanTreeNode> byId)
{
    public IReadOnlyList<SpanTreeNode> Roots { get; } = roots;

    public IReadOnlyList<SpanTreeNode> Nodes { get; } = nodes;

    public int Count => Nodes.Count;

    public SpanTreeNode? Find(string spanId) => byId.TryGetValue(spanId, out var node) ? node : null;
}
