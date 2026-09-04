using System.Text;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console.Rendering;

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
    /// Builds the markup line for one task row, guaranteeing the plain-text
    /// width of the returned line never exceeds <paramref name="maxWidth"/>
    /// display cells. When the row does not fit at full fidelity, segments
    /// degrade in this order, stopping as soon as the line fits: truncate
    /// the title with an ellipsis; drop the title entirely once its budget
    /// falls below 2 cells; truncate the id (keeping its leading
    /// characters) with an ellipsis; drop the priority, then the type code,
    /// to free room for the id. A <paramref name="maxWidth"/> of 0 or less
    /// produces an empty line.
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
        var glyphPlain = TaskGlyphs.Status(status);
        var typeCode = TaskGlyphs.TypeCode(type);
        var priorityText = TaskPriorities.Format(priority);
        var priorityStyle = ThemeTokens.GetStyle($"badge.priority.p{priority}").ToMarkup();

        var prefixWidth = CellWidth(prefix);
        var glyphWidth = CellWidth(glyphPlain);
        var typeWidth = CellWidth(typeCode) + 2; // brackets around the type code
        var priorityWidth = CellWidth(priorityText);

        // Width of prefix + glyph + [type] + priority + id, at full id
        // length, so the title can be truncated to make the whole line fit.
        var widthWithFullId =
            prefixWidth + glyphWidth + 1
            + typeWidth + 1
            + priorityWidth + 1
            + CellWidth(id);

        // Budget left for the title plus its separating space.
        var titleBudget = maxWidth - widthWithFullId - 1;
        var truncatedTitle = titleBudget >= 2 ? TruncateToCells(title, titleBudget) : null;

        var includeType = true;
        var includePriority = true;
        var idText = id;

        if (truncatedTitle is null && widthWithFullId > maxWidth)
        {
            // The fixed segments alone (with the full id) already exceed
            // maxWidth: truncate the id, then drop trailing segments in
            // order (priority, then type) until there is room for it.
            var idBudget = maxWidth - (prefixWidth + glyphWidth + 1 + typeWidth + 1 + priorityWidth + 1);

            if (idBudget < 1)
            {
                includePriority = false;
                idBudget = maxWidth - (prefixWidth + glyphWidth + 1 + typeWidth + 1);
            }

            if (idBudget < 1)
            {
                includeType = false;
                idBudget = maxWidth - (prefixWidth + glyphWidth + 1);
            }

            if (idBudget < 1)
            {
                // Even prefix + glyph + a single separator do not fit. The
                // status glyph is the most informative single character, so
                // keep as much of it as fits and spend any leftover budget
                // on the trailing part of the selection prefix; this only
                // triggers below ~3 cells.
                if (glyphWidth >= maxWidth)
                {
                    return Markup.Escape(CropToCells(glyphPlain, maxWidth));
                }

                var prefixPart = CropToCells(prefix, maxWidth - glyphWidth);
                return Markup.Escape(prefixPart) + TaskGlyphs.StatusMarkup(status);
            }

            idText = TruncateToCells(id, idBudget);
        }

        var sb = new StringBuilder();
        sb.Append(Markup.Escape(prefix)).Append(TaskGlyphs.StatusMarkup(status)).Append(' ');

        if (includeType)
        {
            sb.Append(TaskGlyphs.TypeCodeMarkup(type)).Append(' ');
        }

        if (includePriority)
        {
            sb.Append(Stylize(priorityStyle, priorityText)).Append(' ');
        }

        sb.Append(Markup.Escape(idText));

        if (truncatedTitle is not null)
        {
            sb.Append(' ').Append(Markup.Escape(truncatedTitle));
        }

        var line = sb.ToString();

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";

    /// <summary>
    /// Truncates <paramref name="value"/> to at most <paramref name="cellBudget"/>
    /// display cells, appending an ellipsis (itself 1 cell) when it does not
    /// already fit. Never splits a wide (for example CJK or emoji) grapheme
    /// in half.
    /// </summary>
    private static string TruncateToCells(string value, int cellBudget)
    {
        if (cellBudget <= 0)
        {
            return string.Empty;
        }

        if (CellWidth(value) <= cellBudget)
        {
            return value;
        }

        if (cellBudget == 1)
        {
            return Ellipsis;
        }

        return CropToCells(value, cellBudget - 1) + Ellipsis;
    }

    /// <summary>
    /// Crops <paramref name="value"/> to at most <paramref name="cellBudget"/>
    /// display cells, keeping its leading characters and never splitting a
    /// wide grapheme in half.
    /// </summary>
    private static string CropToCells(string value, int cellBudget)
    {
        if (cellBudget <= 0)
        {
            return string.Empty;
        }

        return Segment.Truncate(new Segment(value), cellBudget)?.Text ?? string.Empty;
    }

    private static int CellWidth(string value) => new Segment(value).CellCount();
}
