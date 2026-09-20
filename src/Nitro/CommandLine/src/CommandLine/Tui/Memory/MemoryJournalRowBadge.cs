using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Renders a journal row with a selection prefix, UTC capture time, and body preview.
/// </summary>
internal static class MemoryJournalRowBadge
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";

    public readonly record struct Widths(int Day);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/>, the rows about to be rendered.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<MemoryJournalEntry> rows)
    {
        var dayWidth = 0;

        foreach (var entry in rows)
        {
            dayWidth = Math.Max(dayWidth, DisplayWidth.Measure(FormatDay(entry.CreatedAt)));
        }

        return new Widths(dayWidth);
    }

    /// <summary>
    /// Builds the markup line for one journal entry row, padding
    /// scope/day to <paramref name="widths"/> and truncating a first-line
    /// preview of the body so the whole line still fits within
    /// <paramref name="maxWidth"/> display columns. A <paramref name="maxWidth"/>
    /// of 0 or less produces an empty line.
    /// </summary>
    public static string Render(
        MemoryJournalEntry entry, bool selected, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var day = DisplayWidth.PadRight(FormatDay(entry.CreatedAt), widths.Day);
        var preview = FirstLine(entry.Body);

        var fixedPlainWidth = DisplayWidth.Measure(prefix) + DisplayWidth.Measure(day) + 1;
        var previewBudget = Math.Max(0, maxWidth - fixedPlainWidth);
        var truncatedPreview = DisplayWidth.Truncate(preview, previewBudget);

        var ageStyle = ThemeTokens.GetStyle("memory.list.age").ToMarkup();

        var line = fixedPlainWidth > maxWidth
            ? RenderNarrow(prefix, maxWidth, day, ageStyle)
            : $"{Markup.Escape(prefix)}"
                + $"{Stylize(ageStyle, Markup.Escape(day))} "
                + Markup.Escape(truncatedPreview);

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

    private static string FormatDay(DateTimeOffset createdAt)
        => createdAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static string FirstLine(string body)
    {
        var newlineIndex = body.IndexOfAny(['\r', '\n']);
        return (newlineIndex < 0 ? body : body[..newlineIndex]).Trim();
    }

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";

    private static string RenderNarrow(string prefix, int maxWidth, string day, string ageStyle)
    {
        var prefixText = DisplayWidth.Slice(prefix, maxWidth);
        var remaining = maxWidth - DisplayWidth.Measure(prefixText);
        var truncatedDay = DisplayWidth.Truncate(day, remaining);

        return Markup.Escape(prefixText) + Stylize(ageStyle, Markup.Escape(truncatedDay));
    }
}
