namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// One cell's already-styled content: the plain text measured and padded for layout, and the
/// Spectre style markup (without brackets) it renders with. An empty style renders unstyled.
/// </summary>
internal readonly record struct TableCellSpec(string Text, string StyleMarkup = "");

/// <summary>
/// Renders a table's fixed top block and rows from a <see cref="TableColumnSpec"/> layout: a
/// leading cursor cell and glyph cell followed by the column cells, each column separated by
/// <see cref="TableLayout.Gutter"/> and padded or truncated to its planned width.
/// </summary>
internal static class TableRenderer
{
    private const char RuleGlyph = '─';

    /// <summary>
    /// Builds the markup line for one row: <paramref name="prefix"/> and <paramref name="glyph"/>
    /// unconditionally, one space, then every column whose <paramref name="layout"/> entry is
    /// visible, padded (or, for the first column when nothing else fits, truncated) to its
    /// planned width and separated by a gutter.
    /// </summary>
    public static string RenderRow(
        string prefix,
        TableCellSpec glyph,
        IReadOnlyList<TableCellSpec> cells,
        IReadOnlyList<TableColumnSpec> columns,
        IReadOnlyList<ColumnLayout> layout)
    {
        var line = Markup.Escape(prefix) + Stylize(glyph.StyleMarkup, Markup.Escape(glyph.Text)) + " ";
        var needsGutter = false;

        for (var i = 0; i < columns.Count; i++)
        {
            if (!layout[i].Visible)
            {
                continue;
            }

            if (needsGutter)
            {
                line += TableLayout.Gutter;
            }

            needsGutter = true;

            var width = layout[i].Width;
            var truncated = DisplayWidth.Truncate(cells[i].Text, width);
            var cellText = columns[i].Alignment == ColumnAlignment.Right
                ? DisplayWidth.PadLeft(truncated, width)
                : DisplayWidth.PadRight(truncated, width);

            line += Stylize(cells[i].StyleMarkup, Markup.Escape(cellText));
        }

        return line;
    }

    /// <summary>
    /// Builds the dashed rule line under the header, filling <paramref name="width"/> display
    /// columns. A non-positive width produces an empty line.
    /// </summary>
    public static string RenderRule(int width, string styleMarkup) =>
        width <= 0 ? string.Empty : Stylize(styleMarkup, new string(RuleGlyph, width));

    /// <summary>
    /// Wraps <paramref name="content"/> in Spectre markup for <paramref name="styleMarkup"/>,
    /// or returns it unchanged when the style is empty.
    /// </summary>
    public static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";
}
