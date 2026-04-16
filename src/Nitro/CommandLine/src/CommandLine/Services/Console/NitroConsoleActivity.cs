using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleActivity : INitroConsoleActivity
{
    private readonly IActivitySink _sink;
    private readonly ActivityEntry _entry;
    private readonly string _failureMessage;
    private readonly NitroConsoleActivity? _parent;
    private bool _completed;

    private NitroConsoleActivity(
        IActivitySink sink,
        ActivityEntry entry,
        string failureMessage,
        NitroConsoleActivity? parent)
    {
        _sink = sink;
        _entry = entry;
        _failureMessage = failureMessage;
        _parent = parent;
    }

    public static INitroConsoleActivity Start(
        IActivitySink sink,
        string title,
        string failureMessage)
    {
        var root = sink.AddRoot(title);
        return new NitroConsoleActivity(sink, root, failureMessage, parent: null);
    }

    public void Update(
        string message,
        ActivityUpdateKind kind = ActivityUpdateKind.Regular,
        IRenderable? details = null)
    {
        if (_completed)
        {
            return;
        }

        var state = MapState(kind);
        var child = _sink.AddChild(_entry, message, state);

        if (details is not null)
        {
            _sink.SetDetails(child, details);
        }
    }

    public void Warning(string message)
    {
        Complete(message, ActivityState.Warning, details: null);
    }

    public void Success(string message)
    {
        Complete(message, ActivityState.Completed, details: null);
    }

    public void Fail(string message)
    {
        Complete(message, ActivityState.Failed, details: null);
    }

    public void Fail()
    {
        Fail(_failureMessage);
    }

    public void Fail(IRenderable details)
    {
        Complete(_failureMessage, ActivityState.Failed, details);
    }

    public async ValueTask FailAllAsync(IRenderable? details = null)
    {
        if (details is not null)
        {
            Complete(_failureMessage, ActivityState.Failed, details);
        }
        else if (!_completed)
        {
            _sink.Fail(_entry, _failureMessage);
            _completed = true;

            if (IsRoot)
            {
                _sink.Stop();
            }
        }

        if (_parent is not null)
        {
            await _parent.FailAllAsync();
        }
        else
        {
            await _sink.Completion;
        }
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        var child = _sink.AddChild(_entry, title, ActivityState.Active);
        return new NitroConsoleActivity(_sink, child, failureMessage, parent: this);
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

        if (IsRoot)
        {
            await _sink.Completion;
        }
    }

    private bool IsRoot => _parent is null;

    private void Complete(string message, ActivityState state, IRenderable? details)
    {
        if (_completed)
        {
            return;
        }

        if (!IsRoot || state is ActivityState.Failed)
        {
            _sink.FailActiveDescendants(_entry);
        }

        ActivityEntry target;
        if (IsRoot || _entry.Children.Count > 0)
        {
            _sink.SetState(_entry, state);
            target = _sink.CompleteChild(_entry, message, state);
        }
        else
        {
            _sink.SetTextAndState(_entry, message, state);
            target = _entry;
        }

        if (details is not null)
        {
            _sink.SetDetails(target, details);
        }

        _completed = true;

        if (IsRoot)
        {
            _sink.Stop();
        }
    }

    private static ActivityState MapState(ActivityUpdateKind kind) => kind switch
    {
        ActivityUpdateKind.Warning => ActivityState.Warning,
        ActivityUpdateKind.Waiting => ActivityState.Waiting,
        ActivityUpdateKind.Success => ActivityState.Completed,
        _ => ActivityState.Info
    };
}
