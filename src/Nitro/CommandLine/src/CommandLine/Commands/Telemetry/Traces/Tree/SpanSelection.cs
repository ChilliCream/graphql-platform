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
            ? new SpanSelectionResult([], 0)
            : Select(
                [root],
                CountSubtree(root),
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

        var subtreeImportance = ComputeSubtreeImportance(roots, maximumDepth);
        var selected = new List<SpanSelectionEntry>(Math.Min(maximumSpans, totalSpanCount));
        var queue = new PriorityQueue<SelectionCandidate, SpanPriority>();

        foreach (var root in roots)
        {
            queue.Enqueue(
                new SelectionCandidate(root, 0),
                SpanPriority.For(root, subtreeImportance));
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

            foreach (var child in OrderedChildren(node, subtreeImportance).Take(maximumChildrenPerParent))
            {
                queue.Enqueue(
                    new SelectionCandidate(child, depth + 1),
                    SpanPriority.For(child, subtreeImportance));
            }
        }

        return new SpanSelectionResult(selected, totalSpanCount);
    }

    private static int CountSubtree(SpanTreeNode root)
    {
        var count = 0;
        var pending = new Stack<SpanTreeNode>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var node = pending.Pop();
            count++;

            foreach (var child in node.Children)
            {
                pending.Push(child);
            }
        }

        return count;
    }

    private static Dictionary<SpanTreeNode, SpanImportance> ComputeSubtreeImportance(
        IReadOnlyList<SpanTreeNode> roots,
        int maximumDepth)
    {
        var importance = new Dictionary<SpanTreeNode, SpanImportance>();

        foreach (var root in roots)
        {
            ComputeSubtreeImportance(root, 0, maximumDepth, importance);
        }

        return importance;
    }

    private static SpanImportance ComputeSubtreeImportance(
        SpanTreeNode node,
        int depth,
        int maximumDepth,
        Dictionary<SpanTreeNode, SpanImportance> importance)
    {
        if (importance.TryGetValue(node, out var existing))
        {
            return existing;
        }

        var best = SpanImportance.For(node.Span);
        if (depth < maximumDepth)
        {
            foreach (var child in node.Children)
            {
                var childImportance = ComputeSubtreeImportance(
                    child,
                    depth + 1,
                    maximumDepth,
                    importance);
                if (childImportance.CompareTo(best) < 0)
                {
                    best = childImportance;
                }
            }
        }

        importance[node] = best;
        return best;
    }

    private readonly record struct SelectionCandidate(SpanTreeNode Node, int Depth);

    private static IEnumerable<SpanTreeNode> OrderedChildren(
        SpanTreeNode node,
        IReadOnlyDictionary<SpanTreeNode, SpanImportance> subtreeImportance)
        => node.Children
            .OrderBy(child => SpanPriority.For(child, subtreeImportance))
            .ThenBy(static child => child.Span.SpanId, StringComparer.Ordinal);

    private readonly record struct SpanImportance(
        int Category,
        double Duration,
        double Start) : IComparable<SpanImportance>
    {
        public static SpanImportance For(TraceSpan span)
            => new(
                GetCategory(span),
                NormalizeDuration(span.DurationMs),
                NormalizeStart(span.Start));

        public int CompareTo(SpanImportance other)
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

            return Start.CompareTo(other.Start);
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

    private readonly record struct SpanPriority(
        int Category,
        double Duration,
        double Start,
        string SpanId) : IComparable<SpanPriority>
    {
        public static SpanPriority For(
            SpanTreeNode node,
            IReadOnlyDictionary<SpanTreeNode, SpanImportance> subtreeImportance)
        {
            var importance = subtreeImportance[node];
            return new(
                importance.Category,
                importance.Duration,
                importance.Start,
                node.Span.SpanId);
        }

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
