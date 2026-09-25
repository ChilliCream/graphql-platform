using System.Globalization;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Renders the Memory table's header row and each row: kind, type, tags, and age columns.
/// Kind is never dropped; narrow widths drop Type first, then Age, identically for the header
/// and the rows so the two always agree. Tags has no drop priority; it is sized to whatever
/// width Kind, Type, and Age leave and truncated with an ellipsis once its text does not fit.
/// The full body is shown only in the row's popover. Built on the shared
/// <see cref="TableLayout"/> and <see cref="TableRenderer"/> table widget.
/// </summary>
internal static class MemoryRowBadge
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string NoType = "-";
    private const string NoTags = "-";
    private const string CuratedKindText = "curated";
    private const string JournalKindText = "journal";

    private const string KindHeader = "KIND";
    private const string TypeHeader = "TYPE";
    private const string TagsHeader = "TAGS";
    private const string AgeHeader = "AGE";

    private const int MinKindWidth = 8;
    private const int MinTypeWidth = 10;
    private const int MinAgeWidth = 10;

    private const string NowLabel = "now";
    private const string JustNowLabel = "just now";
    private const string AgoSuffix = " ago";
    private const string IsoDateFormat = "yyyy-MM-dd";

    // Type drops first, then Age; Kind has no drop priority so it is never dropped. Tags is not
    // part of this list at all: it is sized separately to whatever width Kind, Type, and Age
    // leave, so it always shows, shrinking (and truncating with an ellipsis) instead of
    // dropping.
    private static readonly IReadOnlyList<TableColumnSpec> s_fixedColumns =
    [
        new TableColumnSpec(KindHeader, MinKindWidth),
        new TableColumnSpec(TypeHeader, MinTypeWidth, DropPriority: 0),
        new TableColumnSpec(AgeHeader, MinAgeWidth, DropPriority: 1)
    ];

    private static readonly TableColumnSpec s_tagsColumn = new(TagsHeader, MinWidth: 0);

    /// <summary>
    /// The column widths a set of rows agree on for Kind, Type, and Age: each padded to at
    /// least its minimum width, and wider still when its header title or a row's value needs
    /// more room. Tags has no fixed width; it is sized per call from the space these columns
    /// leave.
    /// </summary>
    public readonly record struct Widths(int Kind, int Type, int Age);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/>, with each column no
    /// narrower than its minimum width or its header title.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<MemoryRow> rows, DateTimeOffset now)
    {
        var rowValues = new List<IReadOnlyList<string>>(rows.Count);

        foreach (var row in rows)
        {
            rowValues.Add([KindText(row), TypeText(row), FormatAge(row.Time, now)]);
        }

        var widths = TableLayout.ComputeWidths(s_fixedColumns, rowValues);
        return new Widths(widths[0], widths[1], widths[2]);
    }

    /// <summary>
    /// Builds the markup line for one row. Columns are padded to <paramref name="widths"/>.
    /// When the full set of columns does not fit within <paramref name="maxWidth"/> display
    /// columns, Type is dropped first, then Age; Kind always remains, and Tags shrinks (as a
    /// last resort, to nothing) to absorb whatever the others leave, truncating its text with
    /// an ellipsis once it no longer fits. A <paramref name="maxWidth"/> of 0 or less produces
    /// an empty line.
    /// </summary>
    public static string Render(MemoryRow row, DateTimeOffset now, bool selected, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var kindStyle = ThemeTokens.GetStyle("memory.list.kind").ToMarkup();
        var typeStyle = ThemeTokens.GetStyle("memory.list.type").ToMarkup();
        var tagsStyle = ThemeTokens.GetStyle("memory.list.tags").ToMarkup();
        var ageStyle = ThemeTokens.GetStyle("memory.list.age").ToMarkup();

        var cells = new TableCellSpec[]
        {
            new(KindText(row), kindStyle),
            new(TypeText(row), typeStyle),
            new(TagsText(row), tagsStyle),
            new(FormatAge(row.Time, now), ageStyle)
        };

        var budget = maxWidth - PrefixWidth(prefix);
        var (columns, layout) = PlanColumns(budget, widths);
        var line = TableRenderer.RenderRow(prefix, new TableCellSpec(string.Empty), cells, columns, layout);

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = TableRenderer.Stylize(highlightStyle, line);
        }

        return line;
    }

    /// <summary>
    /// Builds the header title line shown above the rows: KIND, TYPE, TAGS, AGE, aligned to
    /// <paramref name="widths"/>. Columns are dropped using the same thresholds as
    /// <see cref="Render"/>, so the header always agrees with the rows below it. A
    /// <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string RenderHeader(int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var headerStyle = ThemeTokens.GetStyle("memory.list.header").ToMarkup();
        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var (columns, layout) = PlanColumns(budget, widths);

        return TableRenderer.RenderRow(
            UnselectedPrefix, new TableCellSpec(string.Empty), BuildHeaderCells(columns, headerStyle), columns, layout);
    }

    /// <summary>
    /// Builds the dashed rule line under the header, filling <paramref name="maxWidth"/>
    /// display columns. A <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string RenderRule(int maxWidth)
    {
        var ruleStyle = ThemeTokens.GetStyle("memory.list.age").ToMarkup();
        return TableRenderer.RenderRule(maxWidth, ruleStyle);
    }

    /// <summary>
    /// Appends the Memory table's fixed top block to <paramref name="lines"/>: a blank line,
    /// the header row, the rule and a trailing blank line, keeping only the first
    /// <paramref name="headerLineCount"/> of the four. A <paramref name="maxWidth"/> of 0 or
    /// less appends nothing.
    /// </summary>
    public static void AddHeaderLines(List<string> lines, int headerLineCount, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return;
        }

        var headerStyle = ThemeTokens.GetStyle("memory.list.header").ToMarkup();
        var ruleStyle = ThemeTokens.GetStyle("memory.list.age").ToMarkup();
        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var (columns, layout) = PlanColumns(budget, widths);

        TableRenderer.RenderTopBlock(
            lines,
            headerLineCount,
            maxWidth,
            UnselectedPrefix,
            new TableCellSpec(string.Empty),
            columns,
            layout,
            headerStyle,
            ruleStyle);
    }

    /// <summary>
    /// Plans the four-column layout for <paramref name="budget"/> display columns, in the
    /// display order Kind, Type, Tags, Age: Kind always shows, Type and then Age drop first
    /// when they do not all fit, and Tags is sized to whatever width remains (zero when
    /// nothing does).
    /// </summary>
    private static (IReadOnlyList<TableColumnSpec> Columns, IReadOnlyList<ColumnLayout> Layout) PlanColumns(
        int budget, Widths widths)
    {
        var fixedLayout = TableLayout.Plan(budget, s_fixedColumns, ToWidthList(widths));

        var usedWidth = 0;
        var visibleFixedCount = 0;

        foreach (var column in fixedLayout)
        {
            if (!column.Visible)
            {
                continue;
            }

            usedWidth += column.Width;
            visibleFixedCount++;
        }

        if (visibleFixedCount > 0)
        {
            usedWidth += DisplayWidth.Measure(TableLayout.Gutter) * visibleFixedCount;
        }

        var tagsWidth = Math.Max(0, budget - usedWidth);

        var columns = new List<TableColumnSpec> { s_fixedColumns[0], s_fixedColumns[1], s_tagsColumn, s_fixedColumns[2] };
        var layout = new List<ColumnLayout>
        {
            fixedLayout[0], fixedLayout[1], new ColumnLayout(true, tagsWidth), fixedLayout[2]
        };

        return (columns, layout);
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

    private static int PrefixWidth(string prefix) => DisplayWidth.Measure(prefix) + 1;

    private static IReadOnlyList<int> ToWidthList(Widths widths) => [widths.Kind, widths.Type, widths.Age];

    private static string KindText(MemoryRow row) =>
        row.Kind == MemoryCollectionFilter.Curated ? CuratedKindText : JournalKindText;

    private static string TypeText(MemoryRow row) => row.Type ?? NoType;

    private static string TagsText(MemoryRow row) => row.Tags.Count == 0 ? NoTags : string.Join(",", row.Tags);

    /// <summary>
    /// Formats the elapsed time between <paramref name="value"/> and <paramref name="now"/>:
    /// "just now" under a minute, a relative duration ("26m", "3h", "2d") with an " ago"
    /// suffix, or an absolute date once the row is a week or older.
    /// </summary>
    private static string FormatAge(DateTimeOffset value, DateTimeOffset now)
    {
        var formatted = MailAges.Format(value, now);

        if (formatted == NowLabel)
        {
            return JustNowLabel;
        }

        return IsAbsoluteDate(formatted) ? formatted : formatted + AgoSuffix;
    }

    private static bool IsAbsoluteDate(string formatted) =>
        DateTime.TryParseExact(
            formatted, IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
