using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Renders one dependency or blocks row as a single Spectre markup line: selection prefix, status
/// glyph, dependency type, direction arrow, target id, and title. Blocking dependency types render
/// bold.
/// </summary>
internal static class TaskDetailRowRenderer
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string DependencyArrow = "->";
    private const string BlocksArrow = "<-";

    /// <summary>
    /// Builds the markup line for one row, truncating the title with an
    /// ellipsis so the whole line fits within <paramref name="maxWidth"/>
    /// display columns. A <paramref name="maxWidth"/> of 0 or less produces
    /// an empty line.
    /// </summary>
    public static string Render(TaskDetailRow row, bool selected, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var status = row.Status ?? "unknown";
        var arrow = row.Kind == TaskDetailRowKind.Dependency ? DependencyArrow : BlocksArrow;
        var title = row.Title ?? "(deleted)";

        var fixedPlainWidth = DisplayWidth.Measure(prefix) + DisplayWidth.Measure(TaskGlyphs.Status(status)) + 1
            + DisplayWidth.Measure(row.Type) + 1 + DisplayWidth.Measure(arrow) + 1
            + DisplayWidth.Measure(row.TargetId) + 1;

        var titleBudget = Math.Max(0, maxWidth - fixedPlainWidth);
        var truncatedTitle = DisplayWidth.Truncate(title, titleBudget);

        var line =
            $"{Markup.Escape(prefix)}{TaskGlyphs.StatusMarkup(status)} "
            + $"{Markup.Escape(row.Type)} {Markup.Escape(arrow)} "
            + $"{Markup.Escape(row.TargetId)} {Markup.Escape(truncatedTitle)}";

        if (row.IsBlocking)
        {
            line = $"[bold]{line}[/]";
        }

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = $"[{highlightStyle}]{line}[/]";
        }

        return line;
    }
}
