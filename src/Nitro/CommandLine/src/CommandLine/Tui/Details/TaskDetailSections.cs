using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Builds the plain-text, word-wrapped lines of a task detail body's
/// long-text sections and comments. Lines are unescaped: rendering escapes
/// them via <c>Markup.Escape</c> before display, matching the plain-text
/// (not markdown) v1 body decision.
/// </summary>
internal static class TaskDetailSections
{
    /// <summary>
    /// Builds a header line followed by <paramref name="text"/> word-wrapped
    /// to <paramref name="width"/>, one entry per display line. Returns an
    /// empty list when <paramref name="text"/> is empty, so the caller omits
    /// the section entirely.
    /// </summary>
    public static IReadOnlyList<string> BuildTextSection(string header, string text, int width)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var lines = new List<string> { header };

        foreach (var paragraphLine in text.ReplaceLineEndings("\n").Split('\n'))
        {
            lines.AddRange(WrapLine(paragraphLine, width));
        }

        return lines;
    }

    /// <summary>
    /// Builds the "Comments:" section: each comment's author and timestamp
    /// followed by its text word-wrapped to <paramref name="width"/>, with a
    /// blank line between comments. Returns an empty list when there are no
    /// comments, so the caller omits the section entirely.
    /// </summary>
    public static IReadOnlyList<string> BuildCommentsSection(IReadOnlyList<TaskComment> comments, int width)
    {
        if (comments.Count == 0)
        {
            return [];
        }

        var lines = new List<string> { "Comments:" };

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

        foreach (var word in line.Split(' '))
        {
            if (word.Length > width)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }

                var remaining = word;

                while (remaining.Length > width)
                {
                    result.Add(remaining[..width]);
                    remaining = remaining[width..];
                }

                current.Append(remaining);
                continue;
            }

            if (current.Length == 0)
            {
                current.Append(word);
            }
            else if (current.Length + 1 + word.Length <= width)
            {
                current.Append(' ').Append(word);
            }
            else
            {
                result.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }
}
