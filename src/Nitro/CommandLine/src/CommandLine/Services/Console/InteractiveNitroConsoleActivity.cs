using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class InteractiveNitroConsoleActivity : INitroConsoleActivity
{
    private readonly ActivityTree _tree;
    private readonly ActivityEntry _entry;
    private readonly string _failureMessage;
    private readonly InteractiveNitroConsoleActivity? _parent;
    private readonly IActivityRenderDriver? _driver;
    private bool _completed;

    private InteractiveNitroConsoleActivity(
        ActivityTree tree,
        ActivityEntry entry,
        string failureMessage,
        InteractiveNitroConsoleActivity? parent,
        IActivityRenderDriver? driver)
    {
        _tree = tree;
        _entry = entry;
        _failureMessage = failureMessage;
        _parent = parent;
        _driver = driver;
    }

    private bool IsRoot => _parent is null;

    public void Update(
        string message,
        ActivityUpdateKind kind = ActivityUpdateKind.Regular,
        IRenderable? details = null)
    {
        if (_completed)
        {
            return;
        }

        var state = kind switch
        {
            ActivityUpdateKind.Warning => ActivityState.Warning,
            ActivityUpdateKind.Waiting => ActivityState.Waiting,
            ActivityUpdateKind.Success => ActivityState.Completed,
            _ => ActivityState.Info
        };

        var child = _tree.AddChild(_entry, message, state);
        if (details is not null)
        {
            _tree.SetEntryDetails(child, details);
        }
    }

    public void Warning(string message)
    {
        Complete(message, ActivityState.Warning);
    }

    public void Success(string message)
    {
        Complete(message, ActivityState.Completed);
    }

    public void Fail(string message)
    {
        CompleteFail(message, details: null);
    }

    public void Fail(IRenderable details)
    {
        CompleteFail(_failureMessage, details);
    }

    public void Fail()
    {
        Fail(_failureMessage);
    }

    public async ValueTask FailAllAsync()
    {
        FailSilent();

        if (_parent is not null)
        {
            await _parent.FailAllAsync();
        }
        else if (_driver is not null)
        {
            await _driver.Completion;
        }
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        var childEntry = _tree.AddChild(_entry, title, ActivityState.Active);
        return new InteractiveNitroConsoleActivity(
            _tree,
            childEntry,
            failureMessage,
            parent: this,
            driver: null);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            if (IsRoot)
            {
                Fail();
            }
            else
            {
                await FailAllAsync();
                return;
            }
        }

        if (_driver is not null)
        {
            await _driver.Completion;
        }
    }

    private void Complete(string message, ActivityState state)
    {
        if (_completed)
        {
            return;
        }

        if (!IsRoot)
        {
            _tree.FailActiveDescendants(_entry);
        }

        if (IsRoot || _entry.Children.Count > 0)
        {
            _tree.SetEntryState(_entry, state);
            _tree.AddChild(_entry, message, state);
        }
        else
        {
            _tree.SetEntryTextAndState(_entry, message, state);
        }

        _completed = true;
        _driver?.Stop();
    }

    private void CompleteFail(string message, IRenderable? details)
    {
        if (_completed)
        {
            return;
        }

        _tree.FailActiveDescendants(_entry);

        ActivityEntry failEntry;
        if (IsRoot || _entry.Children.Count > 0)
        {
            _tree.SetEntryState(_entry, ActivityState.Failed);
            failEntry = _tree.AddChild(_entry, message, ActivityState.Failed);
        }
        else
        {
            _tree.SetEntryTextAndState(_entry, message, ActivityState.Failed);
            failEntry = _entry;
        }

        if (details is not null)
        {
            _tree.SetEntryDetails(failEntry, details);
        }

        _completed = true;
        _driver?.Stop();
    }

    private void FailSilent()
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryState(_entry, ActivityState.Failed);
        _tree.FailActiveDescendants(_entry);
        _completed = true;
        _driver?.Stop();
    }

    public static INitroConsoleActivity Start(
        INitroConsole console,
        string title,
        string failureMessage,
        IActivityRenderDriverFactory driverFactory)
    {
        var tree = new ActivityTree();
        var rootEntry = tree.AddRoot(title);
        var driver = driverFactory.Create(console, tree);

        return new InteractiveNitroConsoleActivity(
            tree,
            rootEntry,
            failureMessage,
            parent: null,
            driver);
    }
}
