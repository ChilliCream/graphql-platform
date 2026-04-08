using ChilliCream.Nitro.CommandLine.Helpers;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleActivity(
    INitroConsole console,
    string failureMessage,
    string prefix,
    INitroConsoleActivity? parent)
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

    public void Update(string message, IRenderable details, ActivityUpdateKind kind = ActivityUpdateKind.Regular)
    {
        var glyph = kind switch
        {
            ActivityUpdateKind.Warning => Glyphs.ExclamationMark.Space(),
            ActivityUpdateKind.Waiting => Glyphs.Clock.Space(),
            ActivityUpdateKind.Success => Glyphs.Check.Space(),
            _ => ""
        };
        console.MarkupLine(prefix + "├── " + glyph + message);
        WriteIndented(details, prefix + "│   ");
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

    public async ValueTask FailAllAsync()
    {
        Fail();

        if (parent is not null)
        {
            await parent.FailAllAsync();
        }
    }

    public INitroConsoleActivity StartChildActivity(string title, string failureMessage)
    {
        console.MarkupLine(prefix + "├── " + title);
        return new NitroConsoleActivity(console, failureMessage, prefix + "│   ", this);
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

    public static INitroConsoleActivity Start(
        INitroConsole console,
        string title,
        string failureMessage)
    {
        console.MarkupLine(title);
        return new NitroConsoleActivity(console, failureMessage, "", null);
    }

    private void WriteIndented(IRenderable renderable, string linePrefix)
    {
        var availableWidth = console.Profile.Width - linePrefix.Length;

        if (availableWidth <= 0)
        {
            return;
        }

        var options = RenderOptions.Create(console, console.Profile.Capabilities);
        var segments = renderable.Render(options, availableWidth);

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
