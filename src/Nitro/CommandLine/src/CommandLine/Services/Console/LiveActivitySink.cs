using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class LiveActivitySink : IActivitySink
{
    private readonly INitroConsole _console;
    private readonly ActivityTree _tree = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(100));
    private readonly Task _liveTask;

    public LiveActivitySink(INitroConsole console)
    {
        _console = console;
        _liveTask = RunAsync();
    }

    public Task Completion => _liveTask;

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
        return _tree.AddChild(parent, text, state, isTerminator: true);
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

    public void Stop()
    {
        _timer.Dispose();
    }

    private async Task RunAsync()
    {
        await _console
            .Live(_tree)
            .AutoClear(true)
            .Overflow(VerticalOverflow.Visible)
            .StartAsync(async ctx =>
            {
                while (await _timer.WaitForNextTickAsync())
                {
                    ctx.Refresh();
                }

                ctx.Refresh();
            });

        // Live cleared its rendered region. Re-emit the final tree state as a plain
        // renderable so it lands in scrollback as normal output. This avoids relying
        // on cursor math to merge live output with subsequent stdout/stderr writes,
        // which was the source of rendering bleed on Windows terminals.
        _tree.EmitLivePadding = false;
        _console.Write(_tree);
    }
}
