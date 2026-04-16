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

    public void SetEntryText(ActivityEntry entry, string text)
    {
        lock (_lock)
        {
            entry.Text = text;
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
                RenderEntry(segments, root, prefix: "", connector: "", options, maxWidth);
            }

            return segments;
        }
    }

    private void RenderEntry(
        List<Segment> segments,
        ActivityEntry entry,
        string prefix,
        string connector,
        RenderOptions options,
        int maxWidth)
    {
        segments.Add(new Segment(prefix));
        segments.Add(new Segment(connector));

        var iconWidth = RenderIcon(segments, entry);

        var textStyle = entry.State switch
        {
            ActivityState.Failed => new Style(Color.Red),
            ActivityState.Warning => new Style(Color.Yellow),
            _ => Style.Plain
        };

        var hasContentBelow = entry.Children.Count > 0 || entry.Details is not null;
        string continuationGuide;

        if (connector.Length > 0)
        {
            continuationGuide = (connector == "└── " ? "    " : "│   ")
                + new string(' ', iconWidth);
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

        for (var i = 0; i < entry.Children.Count; i++)
        {
            var child = entry.Children[i];
            var isLast = i == entry.Children.Count - 1 && entry.Details is null;
            var childConnector = isLast ? "└── " : "├── ";
            var childPrefix = prefix + (connector.Length > 0
                ? (connector == "└── " ? "    " : "│   ")
                : "");

            RenderEntry(segments, child, childPrefix, childConnector, options, maxWidth);
        }

        if (entry.Details is not null)
        {
            var detailsPrefix = prefix + (connector.Length > 0
                ? (connector == "└── " ? "    " : "│   ")
                : "");

            RenderDetails(segments, entry.Details, detailsPrefix, options, maxWidth);
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
