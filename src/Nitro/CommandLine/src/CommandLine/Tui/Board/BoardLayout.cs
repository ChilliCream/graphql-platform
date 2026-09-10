namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// Computes how a board's columns are arranged for one frame: a pure function
/// of terminal size, column count, focus, and the maximize toggle, with no
/// dependency on rendering or a console.
/// </summary>
internal static class BoardLayout
{
    /// <summary>
    /// The minimum per-column content width below which columns switch from
    /// a side-by-side grid to a vertically stacked layout.
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
    /// The fewest content rows an equally-shared stacked column needs to be
    /// usable, below which every column sharing the height equally would be
    /// too cramped to read.
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
    /// Stacks every column vertically, sharing the frame's height equally
    /// among them so all are visible at once. Falls back to expanding only
    /// the focused column, with the rest collapsed to a title line, when the
    /// frame is too short to give every column a usable slice.
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
