using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

internal sealed class SpanTreeNode(TraceSpan span)
{
    public TraceSpan Span { get; } = span;

    public SpanTreeNode? Parent { get; internal set; }

    public IReadOnlyList<SpanTreeNode> Children => MutableChildren;

    internal List<SpanTreeNode> MutableChildren { get; } = [];
}
