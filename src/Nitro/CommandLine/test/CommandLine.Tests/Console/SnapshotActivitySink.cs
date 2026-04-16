using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tests.Console;

internal sealed class SnapshotActivitySink : IActivitySink
{
    private readonly INitroConsole _console;
    private readonly ActivityTree _tree = new();

    public SnapshotActivitySink(INitroConsole console)
    {
        _console = console;
    }

    public Task Completion => Task.CompletedTask;

    public ActivityEntry AddRoot(string text)
    {
        return _tree.AddRoot(text);
    }

    public ActivityEntry AddChild(ActivityEntry parent, string text, ActivityState state)
    {
        return _tree.AddChild(parent, text, state);
    }

    public ActivityEntry CompleteChild(ActivityEntry parent, string text, ActivityState state)
    {
        return _tree.AddChild(parent, text, state);
    }

    public void SetState(ActivityEntry entry, ActivityState state)
    {
        _tree.SetEntryState(entry, state);
    }

    public void SetTextAndState(ActivityEntry entry, string text, ActivityState state)
    {
        _tree.SetEntryTextAndState(entry, text, state);
    }

    public void SetDetails(ActivityEntry entry, IRenderable details)
    {
        _tree.SetEntryDetails(entry, details);
    }

    public void FailActiveDescendants(ActivityEntry entry)
    {
        _tree.FailActiveDescendants(entry);
    }

    public void Fail(ActivityEntry entry, string failureMessage)
    {
        _tree.SetEntryState(entry, ActivityState.Failed);
        _tree.FailActiveDescendants(entry);
    }

    public void Stop()
    {
        _console.Write(_tree);
    }
}
