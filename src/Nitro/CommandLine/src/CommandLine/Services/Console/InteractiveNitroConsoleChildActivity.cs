namespace ChilliCream.Nitro.CommandLine;

internal sealed class InteractiveNitroConsoleChildActivity(
    ActivityTree tree,
    ActivityEntry entry,
    string failureMessage,
    INitroConsoleActivity parent)
    : INitroConsoleActivity
{
    private bool _completed;

    public void Update(string message, ActivityUpdateKind kind = ActivityUpdateKind.Regular)
    {
        var state = kind == ActivityUpdateKind.Warning ? ActivityState.Warning : ActivityState.Info;
        tree.AddChild(entry, message, state);
    }

    public void Warning(string message)
    {
        if (_completed)
        {
            return;
        }

        if (entry.Children.Count > 0)
        {
            tree.SetEntryState(entry, ActivityState.Warning);
            tree.AddChild(entry, message, ActivityState.Warning);
        }
        else
        {
            tree.SetEntryTextAndState(entry, message, ActivityState.Warning);
        }

        _completed = true;
    }

    public void Success(string message)
    {
        if (_completed)
        {
            return;
        }

        if (entry.Children.Count > 0)
        {
            tree.SetEntryState(entry, ActivityState.Completed);
            tree.AddChild(entry, message, ActivityState.Completed);
        }
        else
        {
            tree.SetEntryTextAndState(entry, message, ActivityState.Completed);
        }

        _completed = true;
    }

    public void Fail(string message)
    {
        if (_completed)
        {
            return;
        }

        if (entry.Children.Count > 0)
        {
            tree.SetEntryState(entry, ActivityState.Failed);
            tree.AddChild(entry, message, ActivityState.Failed);
        }
        else
        {
            tree.SetEntryTextAndState(entry, message, ActivityState.Failed);
        }

        _completed = true;
    }

    public void Fail()
    {
        Fail(failureMessage);
    }

    public void FailAll()
    {
        Fail();
        parent.FailAll();
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        var childEntry = tree.AddChild(entry, title, ActivityState.Active);
        return new InteractiveNitroConsoleChildActivity(tree, childEntry, failureMessage, this);
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            FailAll();
        }

        return default;
    }
}
