using System.Globalization;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Output;

/// <summary>
/// Builds GitHub-flavoured Markdown documents with frontmatter and pipe tables. Designed
/// for analytical command output that a coding agent will paste back into its context
/// window. Cells are escaped per the GFM table specification.
/// </summary>
internal sealed class MarkdownWriter
{
    private readonly StringBuilder _buffer = new();

    /// <summary>
    /// Emits a YAML frontmatter block. Keys are written in the order they appear and values
    /// are quoted only when they contain a colon, hash, or leading/trailing whitespace.
    /// </summary>
    public MarkdownWriter Frontmatter(IReadOnlyList<KeyValuePair<string, string>> entries)
    {
        _buffer.Append("---\n");
        foreach (var entry in entries)
        {
            _buffer.Append(entry.Key);
            _buffer.Append(": ");
            _buffer.Append(EscapeFrontmatterValue(entry.Value));
            _buffer.Append('\n');
        }
        _buffer.Append("---\n");
        return this;
    }

    /// <summary>
    /// Emits a level-2 heading followed by a blank line.
    /// </summary>
    public MarkdownWriter Heading(string text)
    {
        EnsureBlankLineBefore();
        _buffer.Append("## ");
        _buffer.Append(text);
        _buffer.Append('\n');
        return this;
    }

    /// <summary>
    /// Emits a single line of body text followed by a newline.
    /// </summary>
    public MarkdownWriter Line(string text)
    {
        _buffer.Append(text);
        _buffer.Append('\n');
        return this;
    }

    /// <summary>
    /// Emits a blockquote. Used by error rendering.
    /// </summary>
    public MarkdownWriter Quote(string text)
    {
        EnsureBlankLineBefore();
        _buffer.Append("> ");
        _buffer.Append(text);
        _buffer.Append('\n');
        return this;
    }

    /// <summary>
    /// Emits a GFM pipe table with the given headers and rows. Cells with a pipe character
    /// are escaped. Empty rows render as a single em-dash to keep the column structure
    /// intact.
    /// </summary>
    public MarkdownWriter Table(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (headers.Count == 0)
        {
            return this;
        }

        EnsureBlankLineBefore();

        _buffer.Append('|');
        foreach (var header in headers)
        {
            _buffer.Append(' ');
            _buffer.Append(EscapeCell(header));
            _buffer.Append(" |");
        }
        _buffer.Append('\n');

        _buffer.Append('|');
        for (var i = 0; i < headers.Count; i++)
        {
            _buffer.Append("---|");
        }
        _buffer.Append('\n');

        foreach (var row in rows)
        {
            _buffer.Append('|');
            for (var i = 0; i < headers.Count; i++)
            {
                var cell = i < row.Count ? row[i] : string.Empty;
                _buffer.Append(' ');
                _buffer.Append(EscapeCell(cell));
                _buffer.Append(" |");
            }
            _buffer.Append('\n');
        }

        return this;
    }

    /// <summary>
    /// Emits a blank line so the next block starts on its own line.
    /// </summary>
    public MarkdownWriter BlankLine()
    {
        _buffer.Append('\n');
        return this;
    }

    /// <summary>
    /// Emits a horizontal rule used to separate frontmatter+table sections in multi-payload
    /// commands such as <c>nitro schema usage --coordinate A --coordinate B</c>.
    /// </summary>
    public MarkdownWriter SectionBreak()
    {
        EnsureBlankLineBefore();
        _buffer.Append("---\n");
        return this;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var text = _buffer.ToString();
        return text.TrimEnd('\n');
    }

    private void EnsureBlankLineBefore()
    {
        if (_buffer.Length == 0)
        {
            return;
        }

        if (_buffer[^1] != '\n')
        {
            _buffer.Append('\n');
            _buffer.Append('\n');
            return;
        }

        if (_buffer.Length >= 2 && _buffer[^2] == '\n')
        {
            return;
        }

        _buffer.Append('\n');
    }

    private static string EscapeCell(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.IndexOf('|') < 0 && value.IndexOf('\n') < 0)
        {
            return value;
        }

        return value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }

    private static string EscapeFrontmatterValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        if (value.Contains(':', StringComparison.Ordinal)
            || value.Contains('#', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || char.IsWhiteSpace(value[0])
            || char.IsWhiteSpace(value[^1]))
        {
            var escaped = value.Replace("\"", "\\\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        return value;
    }

    /// <summary>
    /// Formats a long running count with thousands separators using the invariant culture so
    /// that snapshot tests are stable across locales.
    /// </summary>
    public static string FormatCount(long value)
        => value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a percentage from a 0..1 fraction using the invariant culture.
    /// </summary>
    public static string FormatPercent(double value)
        => (value * 100d).ToString("0.##", CultureInfo.InvariantCulture) + "%";

    /// <summary>
    /// Formats a duration in milliseconds using the invariant culture.
    /// </summary>
    public static string FormatMilliseconds(double value)
        => value.ToString("0.##", CultureInfo.InvariantCulture) + "ms";

    /// <summary>
    /// Formats an ISO date for display. <c>null</c> values render as a hyphen.
    /// </summary>
    public static string FormatDate(DateTimeOffset? value)
        => value is null ? "-" : value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a UTC ISO date-time for display.
    /// </summary>
    public static string FormatDateTime(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}
