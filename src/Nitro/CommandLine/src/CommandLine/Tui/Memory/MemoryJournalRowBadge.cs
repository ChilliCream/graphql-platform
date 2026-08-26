using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Renders one journal entry row for the memory list pane when
/// <see cref="MemoryCollectionFilter.Journal"/> is shown: selection prefix,
/// scope, and the UTC capture day and time, so entries already ordered
/// newest first (see <see cref="IMemoryStore.GetRecentJournalAsync"/>) read
/// as a day-grouped log without a separate day-header row per item.
/// </summary>
internal static class MemoryJournalRowBadge
{
    private const string Ellipsis = "…";
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";

    public readonly record struct Widths(int Day);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/> (the
    /// rows about to be rendered, typically just the visible slice), so
    /// every row's columns are padded to the widest value actually on
    /// screen rather than to every entry in the list.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<MemoryJournalEntry> rows)
    {
        var dayWidth = 0;

        foreach (var entry in rows)
        {
            dayWidth = Math.Max(dayWidth, FormatDay(entry.CreatedAt).Length);
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
        var day = FormatDay(entry.CreatedAt).PadRight(widths.Day);
        var preview = FirstLine(entry.Body);

        var fixedPlainLength = prefix.Length + day.Length + 1;
        var previewBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedPreview = Truncate(preview, previewBudget);

        var ageStyle = ThemeTokens.GetStyle("memory.list.age").ToMarkup();

        var line =
            $"{Markup.Escape(prefix)}"
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
