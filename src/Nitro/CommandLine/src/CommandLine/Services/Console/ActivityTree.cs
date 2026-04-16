using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class ActivityTree : Renderable
{
    private const string BranchConnector = "├── ";
    private const string LastConnector = "└── ";
    private const string BranchLane = "│   ";
    private const string LastLane = "    ";

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<ActivityEntry> _rootEntries = [];
    private readonly Spinner _spinner = Spinner.Known.Default;

    public ActivityEntry AddRoot(string text)
    {
        lock (_lock)
        {
            var entry = new ActivityEntry(text);
            _rootEntries.Add(entry);
            return entry;
        }
    }

    public ActivityEntry AddChild(ActivityEntry parent, string text, ActivityState state)
    {
        lock (_lock)
        {
            return parent.AddChild(text, state);
        }
    }

    public void SetEntryState(ActivityEntry entry, ActivityState state)
    {
        lock (_lock)
        {
            entry.State = state;
        }
    }

    public void SetEntryTextAndState(ActivityEntry entry, string text, ActivityState state)
    {
        lock (_lock)
        {
            entry.Text = text;
            entry.State = state;
        }
    }

    public void SetEntryDetails(ActivityEntry entry, IRenderable details)
    {
        lock (_lock)
        {
            entry.Details = details;
        }
    }

    public void FailActiveDescendants(ActivityEntry entry)
    {
        lock (_lock)
        {
            FailActiveDescendantsCore(entry);
        }
    }

    private static void FailActiveDescendantsCore(ActivityEntry entry)
    {
        foreach (var child in entry.Children)
        {
            if (child.State == ActivityState.Active)
            {
                child.State = ActivityState.Failed;
            }

            FailActiveDescendantsCore(child);
        }
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        lock (_lock)
        {
            var segments = new List<Segment>();

            foreach (var root in _rootEntries)
            {
                RenderEntry(segments, root, parentPrefix: "", NodePosition.Root, options, maxWidth);
            }

            // Workaround for a Spectre.Console bug: LiveRenderable.PositionCursor emits
            // `CSI 0 A` when the rendered shape is one line tall, and most terminals treat
            // that as `CSI 1 A` (move cursor up one row) per ECMA-48 default-parameter
            // handling. The result is that a single-line tree drifts up one row on every
            // refresh and overwrites previously-printed output. Forcing the shape to be at
            // least two lines tall keeps Spectre on the `CursorUp(n>=1)` path.
            segments.Add(Segment.LineBreak);

            return segments;
        }
    }

    private void RenderEntry(
        List<Segment> segments,
        ActivityEntry entry,
        string parentPrefix,
        NodePosition position,
        RenderOptions options,
        int maxWidth)
    {
        var connector = ConnectorFor(position);
        var lane = LaneFor(position);
        var icon = IconFor(entry);
        var textStyle = TextStyleFor(entry.State);

        // When a failed entry has more than one child and its last two children are
        // both failed, suppress the entry's own terminator — the preceding child
        // already conveys the failure.
        var visibleChildCount = entry.Children.Count;
        if (entry.State == ActivityState.Failed
            && visibleChildCount > 1
            && entry.Children[visibleChildCount - 1].State == ActivityState.Failed
            && entry.Children[visibleChildCount - 2].State == ActivityState.Failed)
        {
            visibleChildCount--;
        }

        var hasChildren = visibleChildCount > 0;
        var hasDetails = entry.Details is not null;
        var continuationPrefix = BuildContinuationPrefix(
            parentPrefix,
            lane,
            position,
            icon.Width,
            hasChildren,
            hasDetails);

        segments.Add(new Segment(parentPrefix));
        segments.Add(new Segment(connector));
        icon.Write(segments);

        var availableWidth = Math.Max(1, maxWidth - parentPrefix.Length - connector.Length - icon.Width);
        var markup = new Markup(entry.Text, textStyle);
        var textSegments = ((IRenderable)markup).Render(options, availableWidth);

        WritePrefixed(segments, textSegments, continuationPrefix, prefixFirstLine: false);
        segments.Add(Segment.LineBreak);

        var childPrefix = parentPrefix + lane;
        for (var i = 0; i < visibleChildCount; i++)
        {
            var isLastChild = i == visibleChildCount - 1 && !hasDetails;
            var childPosition = isLastChild ? NodePosition.Last : NodePosition.Middle;
            RenderEntry(segments, entry.Children[i], childPrefix, childPosition, options, maxWidth);
        }

        if (entry.Details is not null)
        {
            RenderDetails(segments, entry.Details, childPrefix, options, maxWidth);
        }
    }

    private static void RenderDetails(
        List<Segment> segments,
        IRenderable details,
        string prefix,
        RenderOptions options,
        int maxWidth)
    {
        var availableWidth = maxWidth - prefix.Length;

        if (availableWidth <= 0)
        {
            return;
        }

        var detailSegments = details.Render(options, availableWidth);
        var endedAtLineStart = WritePrefixed(segments, detailSegments, prefix, prefixFirstLine: true);

        if (!endedAtLineStart)
        {
            segments.Add(Segment.LineBreak);
        }
    }

    private static bool WritePrefixed(
        List<Segment> segments,
        IEnumerable<Segment> rendered,
        string continuationPrefix,
        bool prefixFirstLine)
    {
        var atLineStart = prefixFirstLine;

        foreach (var segment in rendered)
        {
            if (segment.IsLineBreak)
            {
                segments.Add(Segment.LineBreak);
                atLineStart = true;
            }
            else
            {
                if (atLineStart)
                {
                    segments.Add(new Segment(continuationPrefix));
                    atLineStart = false;
                }

                segments.Add(segment);
            }
        }

        return atLineStart;
    }

    private static string BuildContinuationPrefix(
        string parentPrefix,
        string lane,
        NodePosition position,
        int iconWidth,
        bool hasChildren,
        bool hasDetails)
    {
        // Root entries use (children || details) to decide whether to draw a vertical
        // guide under the icon; non-root entries use children only. Preserved as-is
        // from the original implementation — changing it would shift layout.
        var drawGuide = position == NodePosition.Root
            ? hasChildren || hasDetails
            : hasChildren;

        var underIcon = drawGuide
            ? "│" + Spaces(iconWidth - 1)
            : Spaces(iconWidth);

        return parentPrefix + lane + underIcon;
    }

    private static string ConnectorFor(NodePosition position) => position switch
    {
        NodePosition.Middle => BranchConnector,
        NodePosition.Last => LastConnector,
        _ => ""
    };

    private static string LaneFor(NodePosition position) => position switch
    {
        NodePosition.Middle => BranchLane,
        NodePosition.Last => LastLane,
        _ => ""
    };

    private static Style TextStyleFor(ActivityState state) => state switch
    {
        ActivityState.Failed => new Style(Color.Red),
        ActivityState.Warning => new Style(Color.Yellow),
        _ => Style.Plain
    };

    private ActivityIcon IconFor(ActivityEntry entry) => entry.State switch
    {
        ActivityState.Active => new ActivityIcon(CurrentSpinnerFrame(entry), new Style(Color.DeepPink1_1, decoration: Decoration.Bold)),
        ActivityState.Completed => new ActivityIcon("✓", new Style(Color.Green, decoration: Decoration.Bold)),
        ActivityState.Failed => new ActivityIcon("✕", new Style(Color.Red, decoration: Decoration.Bold)),
        ActivityState.Warning => new ActivityIcon("!", new Style(Color.Yellow, decoration: Decoration.Bold)),
        ActivityState.Waiting => new ActivityIcon("⏳", new Style(Color.Blue, decoration: Decoration.Bold)),
        _ => ActivityIcon.None
    };

    private string CurrentSpinnerFrame(ActivityEntry entry)
    {
        var frameIndex =
            (int)(entry.Elapsed.TotalMilliseconds / _spinner.Interval.TotalMilliseconds)
            % _spinner.Frames.Count;
        return _spinner.Frames[frameIndex];
    }

    private static string Spaces(int count) => count <= 0 ? "" : new string(' ', count);

    private enum NodePosition
    {
        Root,
        Middle,
        Last
    }

    private readonly record struct ActivityIcon(string Glyph, Style Style, int Width = 2)
    {
        public static ActivityIcon None { get; } = new("", Spectre.Console.Style.Plain, Width: 0);

        public void Write(List<Segment> segments)
        {
            if (Width == 0)
            {
                return;
            }

            segments.Add(new Segment(Glyph, Style));
            segments.Add(new Segment(" "));
        }
    }
}
