using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// Renders one task row as a single Spectre markup line: selection prefix,
/// status glyph, type code, priority, id, and title.
/// </summary>
internal static class TaskBadge
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string Ellipsis = "…";

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

        // Terminal-cell width of everything but the title.
        var fixedPlainWidth = DisplayWidth.Measure(prefix) + DisplayWidth.Measure(glyph) + 1
            + DisplayWidth.Measure(typeCode) + 2 + 1
            + DisplayWidth.Measure(priorityText) + 1
            + DisplayWidth.Measure(id) + 1;

        var titleBudget = Math.Max(0, maxWidth - fixedPlainWidth);
        var truncatedTitle = DisplayWidth.Truncate(title, titleBudget);
        var escapedTitle = Markup.Escape(truncatedTitle);

        var line = fixedPlainWidth > maxWidth
            ? RenderNarrow(prefix, glyph, status, typeCode, type, priorityText, priorityStyle, id, maxWidth)
            : $"{Markup.Escape(prefix)}{TaskGlyphs.StatusMarkup(status)} "
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

    private static string RenderNarrow(
        string prefix,
        string glyph,
        string status,
        string typeCode,
        string type,
        string priority,
        string priorityStyle,
        string id,
        int maxWidth)
    {
        var remaining = maxWidth - DisplayWidth.Measure(Ellipsis);
        var line = string.Empty;

        if (!AppendNarrowPart(ref line, ref remaining, prefix, string.Empty))
        {
            return line + Ellipsis;
        }

        if (!AppendNarrowPart(
                ref line,
                ref remaining,
                glyph,
                ThemeTokens.GetStyle($"status.glyph.{status}").ToMarkup()))
        {
            return line + Ellipsis;
        }

        if (!AppendNarrowPart(ref line, ref remaining, " ", string.Empty))
        {
            return line + Ellipsis;
        }

        if (!AppendNarrowPart(
                ref line,
                ref remaining,
                $"[{typeCode}]",
                ThemeTokens.GetStyle($"badge.type.{type}").ToMarkup()))
        {
            return line + Ellipsis;
        }

        if (!AppendNarrowPart(ref line, ref remaining, " ", string.Empty))
        {
            return line + Ellipsis;
        }

        if (!AppendNarrowPart(ref line, ref remaining, priority, priorityStyle))
        {
            return line + Ellipsis;
        }

        if (!AppendNarrowPart(ref line, ref remaining, " ", string.Empty))
        {
            return line + Ellipsis;
        }

        if (!AppendNarrowPart(ref line, ref remaining, id, string.Empty))
        {
            return line + Ellipsis;
        }

        return line + Ellipsis;
    }

    private static bool AppendNarrowPart(ref string line, ref int remaining, string value, string styleMarkup)
    {
        var truncatedValue = DisplayWidth.Slice(value, remaining);

        if (truncatedValue.Length == 0)
        {
            return value.Length == 0;
        }

        line += Stylize(styleMarkup, Markup.Escape(truncatedValue));
        remaining -= DisplayWidth.Measure(truncatedValue);
        return truncatedValue.Length == value.Length;
    }

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";
}
