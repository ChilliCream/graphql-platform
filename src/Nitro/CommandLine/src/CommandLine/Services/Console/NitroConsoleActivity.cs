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

    public void Update(
        string message,
        ActivityUpdateKind kind = ActivityUpdateKind.Regular,
        IRenderable? details = null)
    {
        if (_completed)
        {
            return;
        }

        var glyph = kind switch
        {
            ActivityUpdateKind.Warning => Glyphs.ExclamationMark.Space(),
            ActivityUpdateKind.Waiting => Glyphs.Clock.Space(),
            ActivityUpdateKind.Success => Glyphs.Check.Space(),
            _ => ""
        };
        if (kind == ActivityUpdateKind.Warning)
        {
            message = message.AsWarning();
        }

        var glyphPad = kind != ActivityUpdateKind.Regular ? "  " : "";
        WriteWrapped(prefix + "├── ", prefix + "│   " + glyphPad, glyph + message);

        if (details is not null)
        {
            WriteIndented(details, prefix + "│   ");
        }
    }

    public void Warning(string message)
    {
        Complete(Glyphs.ExclamationMark.Space() + message.AsWarning());
    }

    public void Success(string message)
    {
        Complete(Glyphs.Check.Space() + message);
    }

    public void Fail(string message)
    {
        Complete(Glyphs.Cross.Space() + message.AsError());
    }

    public void Fail(IRenderable details)
    {
        if (_completed)
        {
            return;
        }

        WriteWrapped(
            prefix + "└── ",
            prefix + "    " + "  ",
            Glyphs.Cross.Space() + failureMessage.AsError());
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
        WriteWrapped(prefix + "├── ", prefix + "│   ", title);
        return new NitroConsoleActivity(console, failureMessage, prefix + "│   ", this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            await FailAllAsync();
        }
    }

    private void Complete(string markupMessage)
    {
        if (_completed)
        {
            return;
        }

        WriteWrapped(prefix + "└── ", prefix + "    " + "  ", markupMessage);
        _completed = true;
    }

    public static INitroConsoleActivity Start(
        INitroConsole console,
        string title,
        string failureMessage)
    {
        var activity = new NitroConsoleActivity(console, failureMessage, "", null);
        activity.WriteWrapped("", "│ ", title);
        return activity;
    }

    private void WriteWrapped(string linePrefix, string continuationPrefix, string markupText)
    {
        var prefixWidth = Math.Max(linePrefix.Length, continuationPrefix.Length);
        var textWidth = console.Profile.Width - prefixWidth;

        if (textWidth <= 0)
        {
            console.MarkupLine(linePrefix + markupText);
            return;
        }

        var markup = new Markup(markupText);
        var options = RenderOptions.Create(console, console.Profile.Capabilities);
        var segments = ((IRenderable)markup).Render(options, textWidth);

        var hasLineBreaks = false;

        foreach (var seg in segments)
        {
            if (seg.IsLineBreak)
            {
                hasLineBreaks = true;
                break;
            }
        }

        if (!hasLineBreaks)
        {
            console.MarkupLine(linePrefix + markupText);
            return;
        }

        var output = new List<Segment>();
        var atLineStart = true;
        var isFirstLine = true;

        foreach (var segment in segments)
        {
            if (segment.IsLineBreak)
            {
                output.Add(Segment.LineBreak);
                atLineStart = true;
                isFirstLine = false;
            }
            else
            {
                if (atLineStart)
                {
                    output.Add(new Segment(isFirstLine ? linePrefix : continuationPrefix));
                    atLineStart = false;
                }

                output.Add(segment);
            }
        }

        if (!atLineStart)
        {
            output.Add(Segment.LineBreak);
        }

        console.Write(new SegmentRenderable(output));
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

        var output = new List<Segment>();
        var atLineStart = true;

        foreach (var segment in segments)
        {
            if (segment.IsLineBreak)
            {
                output.Add(Segment.LineBreak);
                atLineStart = true;
            }
            else
            {
                if (atLineStart)
                {
                    output.Add(new Segment(linePrefix));
                    atLineStart = false;
                }

                output.Add(segment);
            }
        }

        if (!atLineStart)
        {
            output.Add(Segment.LineBreak);
        }

        console.Write(new SegmentRenderable(output));
    }

    private sealed class SegmentRenderable(IReadOnlyList<Segment> segments) : Renderable
    {
        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            return segments;
        }
    }
}
