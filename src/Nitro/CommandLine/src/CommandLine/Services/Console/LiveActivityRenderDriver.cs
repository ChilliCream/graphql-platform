namespace ChilliCream.Nitro.CommandLine;

internal sealed class LiveActivityRenderDriver : IActivityRenderDriver
{
    private readonly PeriodicTimer _timer;
    private readonly Task _liveTask;

    public LiveActivityRenderDriver(INitroConsole console, ActivityTree tree)
    {
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        _liveTask = RunAsync(console, tree);
    }

    public Task Completion => _liveTask;

    public void Stop()
    {
        _timer.Dispose();
    }

    private async Task RunAsync(INitroConsole console, ActivityTree tree)
    {
        await console
            .Live(tree)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Visible)
            .StartAsync(async ctx =>
            {
                while (await _timer.WaitForNextTickAsync())
                {
                    ctx.Refresh();
                }

                ctx.Refresh();
            });

        // ActivityTree pads its output with a trailing blank line to work around a
        // Spectre.Console bug (see ActivityTree.Render). After Live completes, Spectre
        // has left the cursor one row below that padding. Step back onto the padded
        // row so subsequent output overwrites it instead of leaving a visible gap.
        console.Cursor.Move(CursorDirection.Up, 1);
    }
}
