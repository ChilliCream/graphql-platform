namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// Determines column sizes and visibility from the frame dimensions, column count,
/// focused column, and maximize state.
/// </summary>
internal static class BoardLayout
{
    /// <summary>
    /// The minimum per-column frame width for a side-by-side grid.
    /// </summary>
    private const int StackedWidthThreshold = 24;

    /// <summary>
    /// The height, in rows, a collapsed column takes in
    /// <see cref="BoardLayoutKind.Stacked"/>'s too-short-to-share fallback.
    /// </summary>
    private const int CollapsedStackedHeight = 1;

    /// <summary>
    /// The border/padding rows a column's panel spends above and below its
    /// content, mirroring <c>BoardMode.PanelChromeHeight</c>.
    /// </summary>
    private const int StackedPanelChromeHeight = 2;

    /// <summary>
    /// The minimum interior row count required to give every stacked column equal height.
    /// </summary>
    private const int MinStackedInteriorHeight = 3;

    /// <summary>
    /// The smallest per-column height <see cref="BuildStacked"/> requires
    /// before it shares the frame equally across every column.
    /// </summary>
    private const int MinEqualStackedHeight = MinStackedInteriorHeight + StackedPanelChromeHeight;

    /// <summary>
    /// The blank rows <see cref="BoardLayoutKind.Stacked"/> reserves between
    /// consecutive column panels: one row per gap, so <paramref name="columnCount"/>
    /// panels need <c>columnCount - 1</c> separator rows.
    /// </summary>
    private static int SeparatorRows(int columnCount) => Math.Max(0, columnCount - 1);

    /// <summary>
    /// Decides the layout for a frame of the given size over
    /// <paramref name="columnCount"/> columns, with
    /// <paramref name="focusedColumnIndex"/> focused and
    /// <paramref name="maximized"/> indicating whether the focused column's
    /// maximize toggle is on.
    /// </summary>
    public static BoardLayoutDecision Decide(
        int width, int height, int columnCount, int focusedColumnIndex, bool maximized)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        if (columnCount <= 0)
        {
            return new BoardLayoutDecision { Kind = BoardLayoutKind.Grid, Columns = [] };
        }

        var focused = Math.Clamp(focusedColumnIndex, 0, columnCount - 1);

        if (maximized)
        {
            return new BoardLayoutDecision
            {
                Kind = BoardLayoutKind.Maximized,
                Columns = BuildMaximized(width, height, columnCount, focused)
            };
        }

        if (width / columnCount < StackedWidthThreshold)
        {
            return new BoardLayoutDecision
            {
                Kind = BoardLayoutKind.Stacked,
                Columns = BuildStacked(width, height, columnCount, focused)
            };
        }

        return new BoardLayoutDecision
        {
            Kind = BoardLayoutKind.Grid,
            Columns = BuildGrid(width, height, columnCount)
        };
    }

    private static IReadOnlyList<BoardColumnLayout> BuildGrid(int width, int height, int columnCount)
    {
        var baseWidth = width / columnCount;
        var remainder = width % columnCount;
        var columns = new BoardColumnLayout[columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            var columnWidth = baseWidth + (i < remainder ? 1 : 0);
            columns[i] = new BoardColumnLayout(columnWidth, height, Expanded: true);
        }

        return columns;
    }

    private static IReadOnlyList<BoardColumnLayout> BuildMaximized(
        int width, int height, int columnCount, int focused)
    {
        var columns = new BoardColumnLayout[columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            columns[i] = i == focused
                ? new BoardColumnLayout(width, height, Expanded: true)
                : new BoardColumnLayout(0, 0, Expanded: false);
        }

        return columns;
    }

    /// <summary>
    /// Stacks columns with an equal share of the available height when each meets
    /// the minimum height. Otherwise, only the focused column is expanded and the
    /// others receive one title row each.
    /// </summary>
    private static IReadOnlyList<BoardColumnLayout> BuildStacked(
        int width, int height, int columnCount, int focused)
    {
        var separatorRows = SeparatorRows(columnCount);

        return height >= columnCount * MinEqualStackedHeight + separatorRows
            ? BuildStackedEqual(width, height, columnCount, separatorRows)
            : BuildStackedFocusedFallback(width, height, columnCount, focused, separatorRows);
    }

    private static IReadOnlyList<BoardColumnLayout> BuildStackedEqual(
        int width, int height, int columnCount, int separatorRows)
    {
        var distributable = height - separatorRows;
        var baseHeight = distributable / columnCount;
        var remainder = distributable % columnCount;
        var columns = new BoardColumnLayout[columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            var columnHeight = baseHeight + (i < remainder ? 1 : 0);
            columns[i] = new BoardColumnLayout(width, columnHeight, Expanded: true);
        }

        return columns;
    }

    private static IReadOnlyList<BoardColumnLayout> BuildStackedFocusedFallback(
        int width, int height, int columnCount, int focused, int separatorRows)
    {
        var columns = new BoardColumnLayout[columnCount];
        var collapsedTotal = CollapsedStackedHeight * (columnCount - 1);
        var expandedHeight = Math.Max(1, height - collapsedTotal - separatorRows);

        for (var i = 0; i < columnCount; i++)
        {
            columns[i] = i == focused
                ? new BoardColumnLayout(width, expandedHeight, Expanded: true)
                : new BoardColumnLayout(width, CollapsedStackedHeight, Expanded: false);
        }

        return columns;
    }
}
