using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleChildActivity(
    INitroConsole console,
    string failureMessage,
    string prefix)
    : INitroConsoleActivity
{
    private bool _completed;

    public void Update(string message)
    {
        console.MarkupLine(prefix + "├── " + message);
    }

    public void Warning(string message)
    {
        console.MarkupLine(prefix + "├── " + Glyphs.ExclamationMark.Space() + message);
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

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        console.MarkupLine(prefix + "├── " + title);
        return new NitroConsoleChildActivity(console, failureMessage, prefix + "│   ");
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            Fail();
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
