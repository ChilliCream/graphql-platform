namespace ChilliCream.Nitro.CommandLine;

internal sealed class InteractiveNitroConsoleChildActivity : INitroConsoleActivity
{
    private readonly ActivityTree _tree;
    private readonly ActivityEntry _entry;
    private readonly string _failureMessage;
    private bool _completed;

    public InteractiveNitroConsoleChildActivity(
        ActivityTree tree,
        ActivityEntry entry,
        string failureMessage)
    {
        _tree = tree;
        _entry = entry;
        _failureMessage = failureMessage;
    }

    public void Update(string message)
    {
        _tree.AddChild(_entry, message, ActivityState.Info);
    }

    public void Warning(string message)
    {
        _tree.AddChild(_entry, message, ActivityState.Warning);
    }

    public void Success(string message)
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryTextAndState(_entry, message, ActivityState.Completed);
        _completed = true;
    }

    public void Fail(string message)
    {
        if (_completed)
        {
            return;
        }

        _tree.SetEntryTextAndState(_entry, message, ActivityState.Failed);
        _completed = true;
    }

    public void Fail()
    {
        Fail(_failureMessage);
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        var childEntry = _tree.AddChild(_entry, title, ActivityState.Active);
        return new InteractiveNitroConsoleChildActivity(_tree, childEntry, failureMessage);
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            Fail();
        }

        return default;
    }
}
