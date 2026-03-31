namespace ChilliCream.Nitro.CommandLine;

internal sealed class InteractiveNitroConsoleActivity : INitroConsoleActivity
{
    private readonly ActivityTree _tree;
    private readonly ActivityEntry _rootEntry;
    private readonly string _failureMessage;
    private readonly Task _liveTask;
    private readonly PeriodicTimer _refreshTimer;
    private bool _completed;

    private InteractiveNitroConsoleActivity(
        ActivityTree tree,
        ActivityEntry rootEntry,
        string failureMessage,
        Task liveTask,
        PeriodicTimer refreshTimer)
    {
        _tree = tree;
        _rootEntry = rootEntry;
        _failureMessage = failureMessage;
        _liveTask = liveTask;
        _refreshTimer = refreshTimer;
    }

    public void Update(string message)
    {
        _tree.AddChild(_rootEntry, message, ActivityState.Info);
    }

    public void Warning(string message)
    {
        _tree.AddChild(_rootEntry, message, ActivityState.Warning);
    }

    public void Success(string message)
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryTextAndState(_rootEntry, message, ActivityState.Completed);
        _completed = true;
        _refreshTimer.Dispose();
    }

    public void Fail(string message)
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryTextAndState(_rootEntry, message, ActivityState.Failed);
        _completed = true;
        _refreshTimer.Dispose();
    }

    public void Fail()
    {
        Fail(_failureMessage);
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        var childEntry = _tree.AddChild(_rootEntry, title, ActivityState.Active);
        return new InteractiveNitroConsoleChildActivity(_tree, childEntry, failureMessage);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            Fail();
        }

        await _liveTask;
    }

    public static INitroConsoleActivity Start(
        INitroConsole console,
        string title,
        string failureMessage)
    {
        var tree = new ActivityTree();
        var rootEntry = tree.AddRoot(title);
        var refreshTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        var liveTask = console
            .Live(tree)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Visible)
            .StartAsync(async ctx =>
            {
                while (await refreshTimer.WaitForNextTickAsync())
                {
                    ctx.Refresh();
                }

                ctx.Refresh();
            });

        return new InteractiveNitroConsoleActivity(
            tree,
            rootEntry,
            failureMessage,
            liveTask,
            refreshTimer);
    }
}
