using ChilliCream.Nitro.CommandLine.Helpers;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleActivity(INitroConsole console, string failureMessage)
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
        console.MarkupLine("├── " + glyph + message);
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
        WriteIndented(details, "    ");
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

    private void WriteIndented(IRenderable renderable, string linePrefix)
    {
        var options = RenderOptions.Create(console, console.Profile.Capabilities);
        var segments = renderable.Render(options, console.Profile.Width);

        var lineBuffer = new System.Text.StringBuilder();

        foreach (var segment in segments)
        {
            if (segment.IsLineBreak)
            {
                if (lineBuffer.Length > 0)
                {
                    console.MarkupLine(linePrefix + lineBuffer.ToString().EscapeMarkup());
                    lineBuffer.Clear();
                }
            }
            else
            {
                lineBuffer.Append(segment.Text);
            }
        }

        if (lineBuffer.Length > 0)
        {
            console.MarkupLine(linePrefix + lineBuffer.ToString().EscapeMarkup());
        }
    }
}
