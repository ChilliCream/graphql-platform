using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleActivity(INitroConsole console, string failureMessage)
    : INitroConsoleActivity
{
    private bool _completed;

    public void Update(string message)
    {
        console.MarkupLine("├── " + message);
    }

    public void Warning(string message)
    {
        console.MarkupLine("├── " + Glyphs.ExclamationMark.Space() + message);
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

    public ValueTask DisposeAsync()
    {
        Fail();

        return default;
    }

    private void Complete(string message)
    {
        if (_completed)
        {
            return;
        }

        console.MarkupLine("└── " + message);

        _completed = true;
    }

    public static INitroConsoleActivity Start(
        INitroConsole console,
        string title,
        string failureMessage)
    {
        console.MarkupLine(title);

        return new NitroConsoleActivity(console, failureMessage);
    }
}
