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

        var candidates = Discover(roots, maximumDepth);
        candidates.Sort(static (left, right) =>
        {
            var importance = left.Importance.CompareTo(right.Importance);
            return importance != 0
                ? importance
                : left.EncounterOrdinal.CompareTo(right.EncounterOrdinal);
        });

        var selected = new List<SpanSelectionEntry>(Math.Min(maximumSpans, totalSpanCount));
        var selectedNodes = new HashSet<SpanTreeNode>(ReferenceEqualityComparer.Instance);
        var selectedChildCounts = new Dictionary<SpanTreeNode, int>(ReferenceEqualityComparer.Instance);

        foreach (var target in candidates)
        {
            var path = GetPath(target);
            var missing = path.Where(candidate => !selectedNodes.Contains(candidate.Node)).ToArray();

            if (missing.Length == 0 || selected.Count + missing.Length > maximumSpans)
            {
                continue;
            }

            var canSelect = true;
            foreach (var candidate in missing)
            {
                if (candidate.Predecessor is { } predecessor
                    && selectedChildCounts.GetValueOrDefault(predecessor.Node) >= maximumChildrenPerParent)
                {
                    canSelect = false;
                    break;
                }
            }

            if (!canSelect)
            {
                continue;
            }

            foreach (var candidate in missing)
            {
                selectedNodes.Add(candidate.Node);
                selected.Add(new SpanSelectionEntry(candidate.Node, candidate.Depth, selected.Count));

                if (candidate.Predecessor is { } predecessor)
                {
                    selectedChildCounts[predecessor.Node] =
                        selectedChildCounts.GetValueOrDefault(predecessor.Node) + 1;
                }
            }
        }

        return new SpanSelectionResult(selected, totalSpanCount);
    }

    private static List<SelectionCandidate> Discover(
        IReadOnlyList<SpanTreeNode> roots,
        int maximumDepth)
    {
        var candidates = new List<SelectionCandidate>();
        var pending = new Queue<SelectionCandidate>();
        var visited = new HashSet<SpanTreeNode>(ReferenceEqualityComparer.Instance);
        var encounterOrdinal = 0;

        foreach (var root in roots)
        {
            if (visited.Add(root))
            {
                var candidate = new SelectionCandidate(root, 0, null, encounterOrdinal++);
                candidates.Add(candidate);
                pending.Enqueue(candidate);
            }
        }

        while (pending.Count > 0)
        {
            var candidate = pending.Dequeue();
            if (candidate.Depth == maximumDepth)
            {
                continue;
            }

            foreach (var child in candidate.Node.Children)
            {
                if (visited.Add(child))
                {
                    var childCandidate = new SelectionCandidate(
                        child,
                        candidate.Depth + 1,
                        candidate,
                        encounterOrdinal++);
                    candidates.Add(childCandidate);
                    pending.Enqueue(childCandidate);
                }
            }
        }

        return candidates;
    }

    private static SelectionCandidate[] GetPath(SelectionCandidate target)
    {
        var path = new List<SelectionCandidate>();
        var candidate = target;

        while (true)
        {
            path.Add(candidate);
            if (candidate.Predecessor is not { } predecessor)
            {
                break;
            }

            candidate = predecessor;
        }

        path.Reverse();
        return [.. path];
    }

    private static int CountSubtree(SpanTreeNode root)
    {
        var count = 0;
        var pending = new Stack<SpanTreeNode>();
        var visited = new HashSet<SpanTreeNode>(ReferenceEqualityComparer.Instance);
        pending.Push(root);

        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (!visited.Add(node))
            {
                continue;
            }

            count++;
            foreach (var child in node.Children)
            {
                pending.Push(child);
            }
        }

        return count;
    }

    private sealed class SelectionCandidate(
        SpanTreeNode node,
        int depth,
        SelectionCandidate? predecessor,
        int encounterOrdinal)
    {
        public SpanTreeNode Node { get; } = node;

        public int Depth { get; } = depth;

        public SelectionCandidate? Predecessor { get; } = predecessor;

        public int EncounterOrdinal { get; } = encounterOrdinal;

        public SpanImportance Importance { get; } = SpanImportance.For(node.Span);
    }

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
