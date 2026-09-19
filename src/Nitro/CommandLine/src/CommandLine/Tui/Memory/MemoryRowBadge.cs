using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Renders a curated-memory row with a selection prefix, type, tags, and
/// relative modification time.
/// </summary>
internal static class MemoryRowBadge
{
    private const string Ellipsis = "…";
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string NoTags = "-";

    public readonly record struct Widths(int Type, int Age);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/>, the rows about to be rendered.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<MemoryRecord> rows, DateTimeOffset now)
    {
        var typeWidth = 0;
        var ageWidth = 0;

        foreach (var record in rows)
        {
            typeWidth = Math.Max(typeWidth, record.Type.Length);
            ageWidth = Math.Max(ageWidth, MailAges.Format(record.UpdatedAt, now).Length);
        }

        return new Widths(typeWidth, ageWidth);
    }

    /// <summary>
    /// Builds the markup line for one curated memory row, padding
    /// scope/type/age to <paramref name="widths"/> and truncating the tags
    /// column with an ellipsis so the whole line still fits within
    /// <paramref name="maxWidth"/> display columns. A <paramref name="maxWidth"/>
    /// of 0 or less produces an empty line.
    /// </summary>
    public static string Render(MemoryRecord record, DateTimeOffset now, bool selected, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var type = record.Type.PadRight(widths.Type);
        var age = MailAges.Format(record.UpdatedAt, now).PadRight(widths.Age);
        var tagsText = record.Tags.Count == 0 ? NoTags : string.Join(",", record.Tags);

        var fixedPlainLength = prefix.Length
            + type.Length + 1
            + age.Length + 1;

        var tagsBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedTags = Truncate(tagsText, tagsBudget);

        var typeStyle = ThemeTokens.GetStyle("memory.list.type").ToMarkup();
        var tagsStyle = ThemeTokens.GetStyle("memory.list.tags").ToMarkup();
        var ageStyle = ThemeTokens.GetStyle("memory.list.age").ToMarkup();

        var line =
            $"{Markup.Escape(prefix)}"
            + $"{Stylize(typeStyle, Markup.Escape(type))} "
            + $"{Stylize(tagsStyle, Markup.Escape(truncatedTags))} "
            + $"{Stylize(ageStyle, Markup.Escape(age))}";

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";

    private static string Truncate(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (value.Length <= width)
        {
            return value;
        }

        if (width == 1)
        {
            return Ellipsis;
        }

        return string.Concat(value.AsSpan(0, width - 1), Ellipsis);
    }
}
