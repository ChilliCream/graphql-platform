using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Renders one dependency or blocks row as a single Spectre markup line:
/// selection prefix, status glyph, dependency type, direction arrow, target
/// id, and title, mirroring <see cref="TaskBadge"/>'s row conventions.
/// Blocking dependency types are rendered bold to distinguish them from
/// non-blocking ones.
/// </summary>
internal static class TaskDetailRowRenderer
{
    private const string Ellipsis = "…";
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

        var fixedPlainLength = prefix.Length + TaskGlyphs.Status(status).Length + 1
            + row.Type.Length + 1 + arrow.Length + 1
            + row.TargetId.Length + 1;

        var titleBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedTitle = Truncate(title, titleBudget);

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
