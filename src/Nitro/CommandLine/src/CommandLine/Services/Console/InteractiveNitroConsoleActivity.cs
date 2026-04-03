using Spectre.Console.Rendering;

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

    public void Update(string message, ActivityUpdateKind kind = ActivityUpdateKind.Regular)
    {
        var state = kind switch
        {
            ActivityUpdateKind.Warning => ActivityState.Warning,
            ActivityUpdateKind.Waiting => ActivityState.Waiting,
            ActivityUpdateKind.Success => ActivityState.Completed,
            _ => ActivityState.Info
        };
        _tree.AddChild(_rootEntry, message, state);
    }

    public void Warning(string message)
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryState(_rootEntry, ActivityState.Warning);
        _tree.AddChild(_rootEntry, message, ActivityState.Warning);
        _completed = true;
        _refreshTimer.Dispose();
    }

    public void Success(string message)
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryState(_rootEntry, ActivityState.Completed);
        _tree.AddChild(_rootEntry, message, ActivityState.Completed);
        _completed = true;
        _refreshTimer.Dispose();
    }

    public void Fail(string message)
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryState(_rootEntry, ActivityState.Failed);
        _tree.AddChild(_rootEntry, message, ActivityState.Failed);
        _completed = true;
        _refreshTimer.Dispose();
    }

    public void Fail(IRenderable details)
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryState(_rootEntry, ActivityState.Failed);
        var failChild = _tree.AddChild(_rootEntry, _failureMessage, ActivityState.Failed);
        _tree.SetEntryDetails(failChild, details);
        _completed = true;
        _refreshTimer.Dispose();
    }

    public void Fail()
    {
        Fail(_failureMessage);
    }

    public async ValueTask FailAllAsync()
    {
        FailSilent();
        await _liveTask;
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        var childEntry = _tree.AddChild(_rootEntry, title, ActivityState.Active);
        return new InteractiveNitroConsoleChildActivity(_tree, childEntry, failureMessage, this);
    }

    private void FailSilent()
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryState(_rootEntry, ActivityState.Failed);
        _completed = true;
        _refreshTimer.Dispose();
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
