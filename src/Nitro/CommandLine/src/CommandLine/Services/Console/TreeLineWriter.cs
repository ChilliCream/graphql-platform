using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal static class TreeLineWriter
{
    public static void WriteWrapped(
        INitroConsole console,
        string linePrefix,
        string continuationPrefix,
        string markupText)
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

    public static void WriteIndented(
        INitroConsole console,
        IRenderable renderable,
        string linePrefix)
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
