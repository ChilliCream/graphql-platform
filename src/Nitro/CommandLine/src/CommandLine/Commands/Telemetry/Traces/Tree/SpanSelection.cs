using ChilliCream.Nitro.Client.Telemetry.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

internal static class SpanSelection
{
    public const int MaximumOverviewSpans = 96;
    public const int MaximumFocusedSpans = 40;
    public const int MaximumDepth = 8;
    public const int MaximumChildrenPerParent = 24;
    public const double SlowSpanThresholdMs = 10;

    public static SpanSelectionResult SelectOverview(
        SpanTree tree,
        int maximumSpans = MaximumOverviewSpans,
        int maximumDepth = MaximumDepth,
        int maximumChildrenPerParent = MaximumChildrenPerParent)
        => Select(
            tree.Roots,
            tree.Count,
            maximumSpans,
            maximumDepth,
            maximumChildrenPerParent);

    public static SpanSelectionResult SelectFocused(
        SpanTree tree,
        string spanId,
        int maximumSpans = MaximumFocusedSpans,
        int maximumDepth = MaximumDepth,
        int maximumChildrenPerParent = MaximumChildrenPerParent)
    {
        var root = tree.Find(spanId);
        return root is null
            ? new SpanSelectionResult([], tree.Count)
            : Select(
                [root],
                tree.Count,
                maximumSpans,
                maximumDepth,
                maximumChildrenPerParent);
    }

    public static bool IsError(TraceSpan span)
        => span.StatusCode.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
            || span.Events.Any(static traceEvent =>
                traceEvent.Name.Equals("exception", StringComparison.OrdinalIgnoreCase));

    private static SpanSelectionResult Select(
        IReadOnlyList<SpanTreeNode> roots,
        int totalSpanCount,
        int maximumSpans,
        int maximumDepth,
        int maximumChildrenPerParent)
    {
        if (maximumSpans <= 0 || maximumDepth < 0 || maximumChildrenPerParent <= 0)
        {
            return new SpanSelectionResult([], totalSpanCount);
        }

        var selected = new List<SpanSelectionEntry>(Math.Min(maximumSpans, totalSpanCount));
        var queue = new PriorityQueue<SelectionCandidate, SpanPriority>();

        foreach (var root in roots)
        {
            queue.Enqueue(new SelectionCandidate(root, 0), SpanPriority.For(root));
        }

        while (queue.Count > 0 && selected.Count < maximumSpans)
        {
            var candidate = queue.Dequeue();
            var node = candidate.Node;
            var depth = candidate.Depth;

            selected.Add(new SpanSelectionEntry(node, depth, selected.Count));

            if (depth == maximumDepth)
            {
                continue;
            }

            foreach (var child in OrderedChildren(node).Take(maximumChildrenPerParent))
            {
                queue.Enqueue(
                    new SelectionCandidate(child, depth + 1),
                    SpanPriority.For(child));
            }
        }

        return new SpanSelectionResult(selected, totalSpanCount);
    }

    private readonly record struct SelectionCandidate(SpanTreeNode Node, int Depth);

    private static IEnumerable<SpanTreeNode> OrderedChildren(SpanTreeNode node)
        => node.Children
            .OrderBy(static child => SpanPriority.For(child))
            .ThenBy(static child => child.Span.SpanId, StringComparer.Ordinal);

    private readonly record struct SpanPriority(
        int Category,
        double Duration,
        double Start,
        string SpanId) : IComparable<SpanPriority>
    {
        public static SpanPriority For(SpanTreeNode node)
            => new(
                GetCategory(node.Span),
                NormalizeDuration(node.Span.DurationMs),
                NormalizeStart(node.Span.Start),
                node.Span.SpanId);

        public int CompareTo(SpanPriority other)
        {
            var category = Category.CompareTo(other.Category);
            if (category != 0)
            {
                return category;
            }

            var duration = other.Duration.CompareTo(Duration);
            if (duration != 0)
            {
                return duration;
            }

            var start = Start.CompareTo(other.Start);
            return start != 0
                ? start
                : string.Compare(SpanId, other.SpanId, StringComparison.Ordinal);
        }

        private static int GetCategory(TraceSpan span)
            => IsError(span)
                ? 0
                : span.DurationMs >= SlowSpanThresholdMs
                    ? 1
                    : 2;

        private static double NormalizeDuration(double duration)
            => double.IsNaN(duration) ? 0 : duration;

        private static double NormalizeStart(double start)
            => double.IsNaN(start) ? double.MaxValue : start;
    }
}

internal sealed record SpanSelectionResult(
    IReadOnlyList<SpanSelectionEntry> Spans,
    int TotalSpanCount)
{
    public int Count => Spans.Count;
}

internal sealed record SpanSelectionEntry(
    SpanTreeNode Node,
    int Depth,
    int SelectionIndex);
