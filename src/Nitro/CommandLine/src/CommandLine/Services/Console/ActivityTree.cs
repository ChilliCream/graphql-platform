using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class ActivityTree : Renderable
{
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
                RenderEntry(segments, root, prefix: "", isLast: null, options, maxWidth);
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
        string prefix,
        bool? isLast,
        RenderOptions options,
        int maxWidth)
    {
        var connector = isLast switch
        {
            true => "└── ",
            false => "├── ",
            null => ""
        };

        var childIndent = isLast switch
        {
            true => "    ",
            false => "│   ",
            null => ""
        };

        segments.Add(new Segment(prefix));
        segments.Add(new Segment(connector));

        var iconWidth = RenderIcon(segments, entry);

        var textStyle = entry.State switch
        {
            ActivityState.Failed => new Style(Color.Red),
            ActivityState.Warning => new Style(Color.Yellow),
            _ => Style.Plain
        };

        var hasChildrenBelow = entry.Children.Count > 0;
        var hasContentBelow = hasChildrenBelow || entry.Details is not null;
        string continuationGuide;

        if (isLast.HasValue)
        {
            continuationGuide = hasChildrenBelow
                ? childIndent + "│" + new string(' ', Math.Max(iconWidth - 1, 0))
                : childIndent + new string(' ', iconWidth);
        }
        else if (hasContentBelow)
        {
            continuationGuide = "│" + new string(' ', Math.Max(iconWidth - 1, 0));
        }
        else
        {
            continuationGuide = new string(' ', iconWidth);
        }

        var continuationPrefix = prefix + continuationGuide;
        var availableWidth = maxWidth - prefix.Length - connector.Length - iconWidth;

        if (availableWidth <= 0)
        {
            availableWidth = 1;
        }

        var markup = new Markup(entry.Text, textStyle);
        var textSegments = ((IRenderable)markup).Render(options, availableWidth);
        var atLineStart = false;

        foreach (var segment in textSegments)
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

        segments.Add(Segment.LineBreak);

        var childPrefix = prefix + childIndent;

        for (var i = 0; i < entry.Children.Count; i++)
        {
            var child = entry.Children[i];
            var childIsLast = i == entry.Children.Count - 1 && entry.Details is null;
            RenderEntry(segments, child, childPrefix, childIsLast, options, maxWidth);
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
        var atLineStart = true;

        foreach (var segment in detailSegments)
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
                    segments.Add(new Segment(prefix));
                    atLineStart = false;
                }

                segments.Add(segment);
            }
        }

        if (!atLineStart)
        {
            segments.Add(Segment.LineBreak);
        }
    }

    private int RenderIcon(List<Segment> segments, ActivityEntry entry)
    {
        switch (entry.State)
        {
            case ActivityState.Active:
                var frame = _spinner.Frames[
                    (int)(entry.Elapsed.TotalMilliseconds / _spinner.Interval.TotalMilliseconds)
                    % _spinner.Frames.Count];
                segments.Add(new Segment(frame, new Style(Color.DeepPink1_1, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                return 2;

            case ActivityState.Completed:
                segments.Add(new Segment("✓", new Style(Color.Green, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                return 2;

            case ActivityState.Failed:
                segments.Add(new Segment("✕", new Style(Color.Red, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                return 2;

            case ActivityState.Warning:
                segments.Add(new Segment("!", new Style(Color.Yellow, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                return 2;

            case ActivityState.Waiting:
                segments.Add(new Segment("⏳", new Style(Color.Blue, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                return 2;

            case ActivityState.Info:
            default:
                return 0;
        }
    }
}
