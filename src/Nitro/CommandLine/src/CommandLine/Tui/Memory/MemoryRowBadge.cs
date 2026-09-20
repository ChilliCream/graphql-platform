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
            typeWidth = Math.Max(typeWidth, DisplayWidth.Measure(record.Type));
            ageWidth = Math.Max(ageWidth, DisplayWidth.Measure(MailAges.Format(record.UpdatedAt, now)));
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
        var type = DisplayWidth.PadRight(record.Type, widths.Type);
        var age = DisplayWidth.PadRight(MailAges.Format(record.UpdatedAt, now), widths.Age);
        var tagsText = record.Tags.Count == 0 ? NoTags : string.Join(",", record.Tags);

        var fixedPlainWidth = DisplayWidth.Measure(prefix)
            + DisplayWidth.Measure(type) + 1
            + DisplayWidth.Measure(age) + 1;

        var tagsBudget = Math.Max(0, maxWidth - fixedPlainWidth);
        var truncatedTags = DisplayWidth.Truncate(tagsText, tagsBudget);

        var typeStyle = ThemeTokens.GetStyle("memory.list.type").ToMarkup();
        var tagsStyle = ThemeTokens.GetStyle("memory.list.tags").ToMarkup();
        var ageStyle = ThemeTokens.GetStyle("memory.list.age").ToMarkup();

        var line = fixedPlainWidth > maxWidth
            ? RenderNarrow(prefix, maxWidth, type, typeStyle, age, ageStyle)
            : $"{Markup.Escape(prefix)}"
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

    private static string RenderNarrow(
        string prefix,
        int maxWidth,
        string type,
        string typeStyle,
        string age,
        string ageStyle)
    {
        var prefixText = DisplayWidth.Slice(prefix, maxWidth);
        var line = Markup.Escape(prefixText);
        var remaining = maxWidth - DisplayWidth.Measure(prefixText);
        var hasColumn = false;

        AppendNarrowColumn(ref line, ref remaining, ref hasColumn, type, typeStyle);
        AppendNarrowColumn(ref line, ref remaining, ref hasColumn, age, ageStyle);

        return line;
    }

    private static void AppendNarrowColumn(
        ref string line,
        ref int remaining,
        ref bool hasColumn,
        string value,
        string styleMarkup)
    {
        var separatorWidth = hasColumn ? 1 : 0;
        var valueBudget = remaining - separatorWidth;

        if (valueBudget <= 0)
        {
            return;
        }

        var truncatedValue = DisplayWidth.Truncate(value, valueBudget);

        if (truncatedValue.Length == 0)
        {
            return;
        }

        line += (hasColumn ? " " : string.Empty) + Stylize(styleMarkup, Markup.Escape(truncatedValue));
        remaining -= separatorWidth + DisplayWidth.Measure(truncatedValue);
        hasColumn = true;
    }
}
