using System.Diagnostics;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class ActivityEntry
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly List<ActivityEntry> _children = [];

    public ActivityEntry(string text, ActivityState state = ActivityState.Active)
    {
        Text = text;
        State = state;
    }

    public string Text { get; set; }

    public ActivityState State { get; set; }

    public IReadOnlyList<ActivityEntry> Children => _children;

    public IRenderable? Details { get; set; }

    public bool IsTerminator { get; init; }

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public ActivityEntry AddChild(string text, ActivityState state, bool isTerminator = false)
    {
        var child = new ActivityEntry(text, state) { IsTerminator = isTerminator };
        _children.Add(child);
        return child;
    }
}
