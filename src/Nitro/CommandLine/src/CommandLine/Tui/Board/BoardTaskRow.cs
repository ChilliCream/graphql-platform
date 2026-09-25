using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// Renders a board column's task table: a status glyph cell, type code, priority, id, and
/// title columns. Narrow widths drop priority first, then type; id and title are never
/// dropped, with title taking whatever width remains and truncating with an ellipsis. Built
/// on the shared <see cref="TableLayout"/> and <see cref="TableRenderer"/> table widget,
/// reusing <see cref="TaskGlyphs"/> and <see cref="TaskBadge.PriorityStyle"/> for the cell
/// styling.
/// </summary>
internal static class BoardTaskRow
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";

    private const string TypeHeader = "TYPE";
    private const string PriorityHeader = "PRIO";
    private const string IdHeader = "ID";
    private const string TitleHeader = "TITLE";

    private const int MinTypeWidth = 4;
    private const int MinPriorityWidth = 4;
    private const int MinIdWidth = 10;

    /// <summary>
    /// The terminal-cell width every status glyph occupies, used to size the leading cell.
    /// </summary>
    private const int GlyphCellWidth = 1;

    /// <summary>
    /// The header row's stand-in for the glyph cell: blank, but the same width as an actual
    /// status glyph, so the header and rows agree on where the type column starts.
    /// </summary>
    private static readonly string s_blankGlyph = new(' ', GlyphCellWidth);

    // Priority drops first, then type; id has no drop priority so it is never dropped. Title
    // is appended separately at render time, taking whatever width remains after id, type,
    // and priority.
    private static readonly IReadOnlyList<TableColumnSpec> s_columns =
    [
        new TableColumnSpec(TypeHeader, MinTypeWidth, DropPriority: 1),
        new TableColumnSpec(PriorityHeader, MinPriorityWidth, DropPriority: 0, Alignment: ColumnAlignment.Right),
        new TableColumnSpec(IdHeader, MinIdWidth)
    ];

    /// <summary>
    /// The column widths a set of rows agree on: type, priority, and id, each no narrower
    /// than its minimum width or its header title.
    /// </summary>
    public readonly record struct Widths(int Type, int Priority, int Id);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/>, with each column no
    /// narrower than its minimum width or its header title.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<TaskItem> rows)
    {
        var rowValues = new List<IReadOnlyList<string>>(rows.Count);

        foreach (var row in rows)
        {
            rowValues.Add([TaskGlyphs.TypeCode(row.Type), TaskPriorities.Format(row.Priority), row.Id]);
        }

        var widths = TableLayout.ComputeWidths(s_columns, rowValues);
        return new Widths(widths[0], widths[1], widths[2]);
    }

    /// <summary>
    /// Builds the markup line for one task row. Columns are padded to <paramref name="widths"/>.
    /// When the full set of columns does not fit within <paramref name="maxWidth"/> display
    /// columns, priority is dropped first, then type; id always remains, and title fills
    /// whatever space is left, truncated with an ellipsis. A <paramref name="maxWidth"/> of 0
    /// or less produces an empty line.
    /// </summary>
    public static string Render(TaskItem task, bool selected, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var glyph = TaskGlyphs.Status(task.Status);
        var glyphStyle = ThemeTokens.GetStyle($"status.glyph.{task.Status}").ToMarkup();
        var typeStyle = ThemeTokens.GetStyle($"badge.type.{task.Type}").ToMarkup();
        var priorityStyle = TaskBadge.PriorityStyle(task.Priority);

        var cells = new TableCellSpec[]
        {
            new(TaskGlyphs.TypeCode(task.Type), typeStyle),
            new(TaskPriorities.Format(task.Priority), priorityStyle),
            new(task.Id, string.Empty),
            new(task.Title, string.Empty)
        };

        var budget = maxWidth - PrefixWidth(prefix);
        var (columns, layout) = PlanWithTitle(budget, widths);
        var line = TableRenderer.RenderRow(prefix, new TableCellSpec(glyph, glyphStyle), cells, columns, layout);

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = TableRenderer.Stylize(highlightStyle, line);
        }

        return line;
    }

    /// <summary>
    /// Builds the header title line shown above the rows: TYPE, PRIO, ID, TITLE, aligned to
    /// <paramref name="widths"/> with the cursor and glyph cells left blank. Columns are
    /// dropped using the same thresholds as <see cref="Render"/>, so the header always agrees
    /// with the rows below it. A <paramref name="maxWidth"/> of 0 or less produces an empty
    /// line.
    /// </summary>
    public static string RenderHeader(int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var headerStyle = ThemeTokens.GetStyle("board.column.header").ToMarkup();
        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var (columns, layout) = PlanWithTitle(budget, widths);
        var headerCells = BuildHeaderCells(columns, headerStyle);

        return TableRenderer.RenderRow(
            UnselectedPrefix, new TableCellSpec(s_blankGlyph), headerCells, columns, layout);
    }

    /// <summary>
    /// Builds the dashed rule line under the header, filling <paramref name="maxWidth"/>
    /// display columns. A <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string RenderRule(int maxWidth)
    {
        var ruleStyle = ThemeTokens.GetStyle("board.column.border").ToMarkup();
        return TableRenderer.RenderRule(maxWidth, ruleStyle);
    }

    /// <summary>
    /// Appends the task table's fixed top block to <paramref name="lines"/>: a blank line,
    /// the header row, the rule, and a trailing blank line, keeping only the first
    /// <paramref name="headerLineCount"/> of the four. A <paramref name="maxWidth"/> of 0 or
    /// less appends nothing.
    /// </summary>
    public static void AddHeaderLines(List<string> lines, int headerLineCount, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return;
        }

        var headerStyle = ThemeTokens.GetStyle("board.column.header").ToMarkup();
        var ruleStyle = ThemeTokens.GetStyle("board.column.border").ToMarkup();
        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var (columns, layout) = PlanWithTitle(budget, widths);

        TableRenderer.RenderTopBlock(
            lines,
            headerLineCount,
            maxWidth,
            UnselectedPrefix,
            new TableCellSpec(s_blankGlyph),
            columns,
            layout,
            headerStyle,
            ruleStyle);
    }

    /// <summary>
    /// Plans type, priority, and id against <paramref name="budget"/>, then appends a title
    /// column sized to whatever width remains after the visible columns and their gutters.
    /// </summary>
    private static (IReadOnlyList<TableColumnSpec> Columns, IReadOnlyList<ColumnLayout> Layout) PlanWithTitle(
        int budget, Widths widths)
    {
        var layout = TableLayout.Plan(budget, s_columns, ToWidthList(widths));
        var titleWidth = ComputeTitleWidth(budget, layout);

        var columns = new List<TableColumnSpec>(s_columns) { new(TitleHeader, 0, FixedWidth: titleWidth) };
        var fullLayout = new List<ColumnLayout>(layout) { new(true, titleWidth) };

        return (columns, fullLayout);
    }

    private static int ComputeTitleWidth(int budget, IReadOnlyList<ColumnLayout> layout)
    {
        var usedWidth = 0;
        var visibleCount = 0;

        foreach (var column in layout)
        {
            if (!column.Visible)
            {
                continue;
            }

            usedWidth += column.Width;
            visibleCount++;
        }

        // One gutter before each visible type/priority/id column and one more before title.
        usedWidth += visibleCount * DisplayWidth.Measure(TableLayout.Gutter);

        return Math.Max(0, budget - usedWidth);
    }

    private static TableCellSpec[] BuildHeaderCells(IReadOnlyList<TableColumnSpec> columns, string headerStyle)
    {
        var cells = new TableCellSpec[columns.Count];

        for (var i = 0; i < columns.Count; i++)
        {
            cells[i] = new TableCellSpec(columns[i].Title, headerStyle);
        }

        return cells;
    }

    private static int PrefixWidth(string prefix) => DisplayWidth.Measure(prefix) + GlyphCellWidth + 1;

    private static IReadOnlyList<int> ToWidthList(Widths widths) => [widths.Type, widths.Priority, widths.Id];
}
