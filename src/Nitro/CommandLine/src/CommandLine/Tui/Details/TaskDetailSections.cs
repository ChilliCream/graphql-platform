using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Builds the plain-text, word-wrapped lines of a task detail body's long-text sections and comments.
/// Lines are unescaped: rendering escapes them via <c>Markup.Escape</c> before display.
/// </summary>
internal static class TaskDetailSections
{
    /// <summary>
    /// Builds a header line followed by <paramref name="text"/> word-wrapped to <paramref name="width"/>,
    /// one entry per display line. Returns an empty list when <paramref name="text"/> is empty.
    /// </summary>
    public static IReadOnlyList<string> BuildTextSection(string header, string text, int width)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var lines = new List<string> { header };
        lines.AddRange(WrapText(text, width));
        return lines;
    }

    /// <summary>
    /// Word-wraps <paramref name="text"/> to <paramref name="width"/> display
    /// columns, one entry per display line, preserving the source paragraph
    /// breaks (each line of <paramref name="text"/> wraps independently).
    /// </summary>
    public static IReadOnlyList<string> WrapText(string text, int width)
    {
        var lines = new List<string>();

        foreach (var paragraphLine in text.ReplaceLineEndings("\n").Split('\n'))
        {
            lines.AddRange(WrapLine(paragraphLine, width));
        }

        return lines;
    }

    /// <summary>
    /// Builds the "Comments" section: each comment's author and timestamp followed by its text
    /// word-wrapped to <paramref name="width"/>, with a blank line between comments. Returns an empty
    /// list when there are no comments.
    /// </summary>
    public static IReadOnlyList<string> BuildCommentsSection(IReadOnlyList<TaskComment> comments, int width)
    {
        if (comments.Count == 0)
        {
            return [];
        }

        var lines = new List<string> { "Comments" };

        for (var i = 0; i < comments.Count; i++)
        {
            if (i > 0)
            {
                lines.Add(string.Empty);
            }

            var comment = comments[i];
            lines.Add($"{comment.Author} - {TaskDates.Format(comment.CreatedAt)}");

            foreach (var paragraphLine in comment.Text.ReplaceLineEndings("\n").Split('\n'))
            {
                lines.AddRange(WrapLine(paragraphLine, width));
            }
        }

        return lines;
    }

    /// <summary>
    /// Greedily word-wraps one line of text to <paramref name="width"/>
    /// display columns. A word longer than <paramref name="width"/> is
    /// hard-broken across lines. A <paramref name="width"/> of 0 or less
    /// returns the line unwrapped, and an empty line returns one empty entry.
    /// </summary>
    public static IReadOnlyList<string> WrapLine(string line, int width)
    {
        if (width <= 0)
        {
            return [line];
        }

        if (line.Length == 0)
        {
            return [string.Empty];
        }

        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var currentWidth = 0;

        foreach (var word in line.Split(' '))
        {
            var wordWidth = DisplayWidth.Measure(word);

            if (wordWidth > width)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    currentWidth = 0;
                }

                var remaining = word;

                while (DisplayWidth.Measure(remaining) > width)
                {
                    var segment = DisplayWidth.Slice(remaining, width);

                    if (segment.Length == 0)
                    {
                        segment = DisplayWidth.FirstTextElement(remaining);
                    }

                    result.Add(segment);
                    remaining = remaining[segment.Length..];
                }

                current.Append(remaining);
                currentWidth = DisplayWidth.Measure(remaining);
                continue;
            }

            if (current.Length == 0)
            {
                current.Append(word);
                currentWidth = wordWidth;
            }
            else if (currentWidth + 1 + wordWidth <= width)
            {
                current.Append(' ').Append(word);
                currentWidth += 1 + wordWidth;
            }
            else
            {
                result.Add(current.ToString());
                current.Clear();
                current.Append(word);
                currentWidth = wordWidth;
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }
}
