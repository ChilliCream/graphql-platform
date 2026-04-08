using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class InteractiveNitroConsoleChildActivity(
    ActivityTree tree,
    ActivityEntry entry,
    string failureMessage,
    INitroConsoleActivity parent)
    : INitroConsoleActivity
{
    private bool _completed;

    public void Update(
        string message,
        ActivityUpdateKind kind = ActivityUpdateKind.Regular,
        IRenderable? details = null)
    {
        var state = kind switch
        {
            ActivityUpdateKind.Warning => ActivityState.Warning,
            ActivityUpdateKind.Waiting => ActivityState.Waiting,
            ActivityUpdateKind.Success => ActivityState.Completed,
            _ => ActivityState.Info
        };
        var child = tree.AddChild(entry, message, state);
        if (details is not null)
        {
            tree.SetEntryDetails(child, details);
        }
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

    public void Fail(IRenderable details)
    {
        if (_completed)
        {
            return;
        }

        ActivityEntry failEntry;

        if (entry.Children.Count > 0)
        {
            tree.SetEntryState(entry, ActivityState.Failed);
            failEntry = tree.AddChild(entry, failureMessage, ActivityState.Failed);
        }
        else
        {
            tree.SetEntryTextAndState(entry, failureMessage, ActivityState.Failed);
            failEntry = entry;
        }

        tree.SetEntryDetails(failEntry, details);
        _completed = true;
    }

    public void Fail()
    {
        Fail(failureMessage);
    }

    public async ValueTask FailAllAsync()
    {
        Fail();
        await parent.FailAllAsync();
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        var childEntry = tree.AddChild(entry, title, ActivityState.Active);
        return new InteractiveNitroConsoleChildActivity(tree, childEntry, failureMessage, this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            await FailAllAsync();
        }
    }
}
