using ChilliCream.Nitro.CommandLine.Helpers;
using Spectre.Console.Rendering;

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
        var glyph = kind switch
        {
            ActivityUpdateKind.Warning => Glyphs.ExclamationMark.Space(),
            ActivityUpdateKind.Waiting => Glyphs.Clock.Space(),
            ActivityUpdateKind.Success => Glyphs.Check.Space(),
            _ => ""
        };
        console.MarkupLine(prefix + "├── " + glyph + message);
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

        console.MarkupLine(prefix + "└── " + Glyphs.Cross.Space() + failureMessage);
        WriteIndented(details, prefix + "    ");
        _completed = true;
    }

    public void Fail()
    {
        Fail(failureMessage);
    }

    public ValueTask FailAllAsync()
    {
        Fail();
        return parent.FailAllAsync();
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        console.MarkupLine(prefix + "├── " + title);
        return new NitroConsoleChildActivity(console, failureMessage, prefix + "│   ", this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            await FailAllAsync();
        }
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

    private void WriteIndented(IRenderable renderable, string linePrefix)
    {
        var writer = new StringWriter();
        var tempConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        tempConsole.Write(renderable);

        var output = writer.ToString();
        foreach (var line in output.TrimEnd().Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.Length > 0)
            {
                console.MarkupLine(linePrefix + trimmed.EscapeMarkup());
            }
        }
    }
}
