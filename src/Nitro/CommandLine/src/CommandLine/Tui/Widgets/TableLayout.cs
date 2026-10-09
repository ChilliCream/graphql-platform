namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// How a <see cref="TableColumnSpec"/>'s cell text is justified within its computed width.
/// </summary>
internal enum ColumnAlignment
{
    Left,
    Right
}

/// <summary>
/// Describes one column of a table rendered with <see cref="TableLayout"/> and
/// <see cref="TableRenderer"/>: its title, minimum width, and how it is dropped or sized once
/// the table is narrower than every column needs. A column with no <see cref="DropPriority"/>
/// is never dropped; among droppable columns, the lowest priority value drops first.
/// </summary>
internal sealed record TableColumnSpec(
    string Title,
    int MinWidth,
    int? DropPriority = null,
    ColumnAlignment Alignment = ColumnAlignment.Left,
    int? FixedWidth = null);

/// <summary>
/// Whether and how wide one column renders once <see cref="TableLayout.Plan"/> has fit the
/// table to the available width.
/// </summary>
internal readonly record struct ColumnLayout(bool Visible, int Width);

/// <summary>
/// Computes column widths from a table's columns and rows, and decides which columns fit a
/// given width: a gutter of four spaces separates columns, and one space of left padding
/// precedes the first one.
/// </summary>
internal static class TableLayout
{
    public const string Gutter = "    ";

    /// <summary>
    /// Returns each column's width, in <paramref name="columns"/> order: its fixed width when
    /// it has one, otherwise at least its minimum width or its title's width, and wider still
    /// when a row's value in that column needs more room.
    /// </summary>
    public static IReadOnlyList<int> ComputeWidths(
        IReadOnlyList<TableColumnSpec> columns, IReadOnlyList<IReadOnlyList<string>> rowValues)
    {
        var widths = new int[columns.Count];

        for (var i = 0; i < columns.Count; i++)
        {
            widths[i] = columns[i].FixedWidth
                ?? Math.Max(columns[i].MinWidth, DisplayWidth.Measure(columns[i].Title));
        }

        foreach (var row in rowValues)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                if (columns[i].FixedWidth is null)
                {
                    widths[i] = Math.Max(widths[i], DisplayWidth.Measure(row[i]));
                }
            }
        }

        return widths;
    }

    /// <summary>
    /// Decides which columns fit <paramref name="budget"/> display columns given their
    /// <paramref name="widths"/>: droppable columns are removed lowest priority first until
    /// the rest fit, and when even the never-dropped columns do not fit, the first remaining
    /// column is truncated to what space is left.
    /// </summary>
    public static IReadOnlyList<ColumnLayout> Plan(
        int budget, IReadOnlyList<TableColumnSpec> columns, IReadOnlyList<int> widths)
    {
        var visible = new bool[columns.Count];

        for (var i = 0; i < columns.Count; i++)
        {
            visible[i] = true;
        }

        while (!Fits(budget, widths, visible) && DropLowestPriority(columns, visible))
        {
        }

        var layouts = new ColumnLayout[columns.Count];

        for (var i = 0; i < columns.Count; i++)
        {
            layouts[i] = new ColumnLayout(visible[i], widths[i]);
        }

        TruncateFirstVisibleIfNeeded(budget, widths, visible, layouts);

        return layouts;
    }

    private static bool Fits(int budget, IReadOnlyList<int> widths, bool[] visible)
    {
        var required = 0;
        var visibleCount = 0;

        for (var i = 0; i < widths.Count; i++)
        {
            if (!visible[i])
            {
                continue;
            }

            required += widths[i];
            visibleCount++;
        }

        if (visibleCount > 1)
        {
            required += DisplayWidth.Measure(Gutter) * (visibleCount - 1);
        }

        return required <= budget;
    }

    private static bool DropLowestPriority(IReadOnlyList<TableColumnSpec> columns, bool[] visible)
    {
        var dropIndex = -1;
        var lowestPriority = int.MaxValue;

        for (var i = 0; i < columns.Count; i++)
        {
            if (!visible[i] || columns[i].DropPriority is not { } priority)
            {
                continue;
            }

            if (priority < lowestPriority)
            {
                lowestPriority = priority;
                dropIndex = i;
            }
        }

        if (dropIndex < 0)
        {
            return false;
        }

        visible[dropIndex] = false;
        return true;
    }

    private static void TruncateFirstVisibleIfNeeded(
        int budget, IReadOnlyList<int> widths, bool[] visible, ColumnLayout[] layouts)
    {
        if (Fits(budget, widths, visible))
        {
            return;
        }

        var firstVisible = Array.IndexOf(visible, true);

        if (firstVisible < 0)
        {
            return;
        }

        var otherWidth = 0;
        var visibleCount = 0;

        for (var i = 0; i < widths.Count; i++)
        {
            if (!visible[i])
            {
                continue;
            }

            visibleCount++;

            if (i != firstVisible)
            {
                otherWidth += widths[i];
            }
        }

        var gutterWidth = visibleCount > 1 ? DisplayWidth.Measure(Gutter) * (visibleCount - 1) : 0;
        var truncatedWidth = Math.Max(0, budget - otherWidth - gutterWidth);

        layouts[firstVisible] = new ColumnLayout(true, truncatedWidth);
    }
}
