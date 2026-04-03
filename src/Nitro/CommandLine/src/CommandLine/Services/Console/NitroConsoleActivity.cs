using ChilliCream.Nitro.CommandLine.Helpers;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleActivity(INitroConsole console, string failureMessage)
    : INitroConsoleActivity
{
    private bool _completed;

    public void Update(string message, ActivityUpdateKind kind = ActivityUpdateKind.Regular)
    {
        var prefix = kind == ActivityUpdateKind.Warning
            ? "├── " + Glyphs.ExclamationMark.Space()
            : "├── ";
        console.MarkupLine(prefix + message);
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

    public void Fail(IRenderable details)
    {
        if (_completed)
        {
            return;
        }

        console.MarkupLine("└── " + Glyphs.Cross.Space() + failureMessage);
        console.Write(details);
        _completed = true;
    }

    public void Fail()
    {
        Fail(failureMessage);
    }

    public ValueTask FailAllAsync()
    {
        Fail();
        return default;
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        console.MarkupLine("├── " + title);
        return new NitroConsoleChildActivity(console, failureMessage, "│   ", this);
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
