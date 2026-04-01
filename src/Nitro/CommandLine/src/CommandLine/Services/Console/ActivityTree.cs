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

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        lock (_lock)
        {
            var segments = new List<Segment>();

            foreach (var root in _rootEntries)
            {
                RenderEntry(segments, root, prefix: "", connector: "");
            }

            return segments;
        }
    }

    private void RenderEntry(
        List<Segment> segments,
        ActivityEntry entry,
        string prefix,
        string connector)
    {
        segments.Add(new Segment(prefix));
        segments.Add(new Segment(connector));

        RenderIcon(segments, entry);

        segments.Add(new Segment(entry.Text));
        segments.Add(Segment.LineBreak);

        for (var i = 0; i < entry.Children.Count; i++)
        {
            var child = entry.Children[i];
            var isLast = i == entry.Children.Count - 1;
            var childConnector = isLast ? "└── " : "├── ";
            var childPrefix = prefix + (connector.Length > 0
                ? (connector == "└── " ? "    " : "│   ")
                : "");

            RenderEntry(segments, child, childPrefix, childConnector);
        }
    }

    private void RenderIcon(List<Segment> segments, ActivityEntry entry)
    {
        switch (entry.State)
        {
            case ActivityState.Active:
                var frame = _spinner.Frames[
                    (int)(entry.Elapsed.TotalMilliseconds / _spinner.Interval.TotalMilliseconds)
                    % _spinner.Frames.Count];
                segments.Add(new Segment(frame, new Style(Color.DeepPink1_1, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                break;

            case ActivityState.Completed:
                segments.Add(new Segment("✓", new Style(Color.Green, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                break;

            case ActivityState.Failed:
                segments.Add(new Segment("✕", new Style(Color.Red, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                break;

            case ActivityState.Warning:
                segments.Add(new Segment("!", new Style(Color.Yellow, decoration: Decoration.Bold)));
                segments.Add(new Segment(" "));
                break;

            case ActivityState.Info:
                break;
        }
    }
}
