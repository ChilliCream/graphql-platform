using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleChildActivity(
    INitroConsole console,
    string failureMessage,
    string prefix,
    INitroConsoleActivity parent)
    : INitroConsoleActivity
{
    private bool _completed;

    public void Update(string message, ActivityUpdateKind kind = ActivityUpdateKind.Regular)
    {
        var linePrefix = kind == ActivityUpdateKind.Warning
            ? prefix + "├── " + Glyphs.ExclamationMark.Space()
            : prefix + "├── ";
        console.MarkupLine(linePrefix + message);
    }

    public void Warning(string message)
    {
        Complete(Glyphs.ExclamationMark.Space() + message);
    }

    public void Success(string message)
    {
        Complete(Glyphs.Check.Space() + message);
    }

    public void Fail(string message)
    {
        Complete(Glyphs.Cross.Space() + message);
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
        console.MarkupLine(prefix + "├── " + title);
        return new NitroConsoleChildActivity(console, failureMessage, prefix + "│   ", this);
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            FailAll();
        }

        return default;
    }

    private void Complete(string message)
    {
        if (_completed)
        {
            return;
        }

        console.MarkupLine(prefix + "└── " + message);
        _completed = true;
    }
}
