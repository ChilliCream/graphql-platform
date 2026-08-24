using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// Renders one task row as a single Spectre markup line: selection prefix,
/// status glyph, type code, priority, id, and title.
/// </summary>
internal static class TaskBadge
{
    private const string Ellipsis = "…";
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";

    /// <summary>
    /// Builds the markup line for one task row, truncating the title with an
    /// ellipsis so the whole line fits within <paramref name="maxWidth"/>
    /// display columns. A <paramref name="maxWidth"/> of 0 or less produces
    /// an empty line.
    /// </summary>
    public static string Render(
        string id,
        string title,
        string status,
        int priority,
        string type,
        bool selected,
        int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var glyph = TaskGlyphs.Status(status);
        var typeCode = TaskGlyphs.TypeCode(type);
        var priorityText = TaskPriorities.Format(priority);
        var priorityStyle = ThemeTokens.GetStyle($"badge.priority.p{priority}").ToMarkup();

        // Plain-text length of everything but the title, so the title can be
        // truncated to make the whole line fit maxWidth.
        var fixedPlainLength = prefix.Length + glyph.Length + 1
            + typeCode.Length + 2 + 1
            + priorityText.Length + 1
            + id.Length + 1;

        var titleBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedTitle = Truncate(title, titleBudget);
        var escapedTitle = Markup.Escape(truncatedTitle);

        var line =
            $"{Markup.Escape(prefix)}{TaskGlyphs.StatusMarkup(status)} "
            + $"{TaskGlyphs.TypeCodeMarkup(type)} "
            + $"{Stylize(priorityStyle, priorityText)} "
            + $"{Markup.Escape(id)} {escapedTitle}";

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
