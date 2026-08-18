namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// The arrangement a <see cref="BoardLayoutDecision"/> resolves to for one frame.
/// </summary>
internal enum BoardLayoutKind
{
    /// <summary>Every column is rendered side by side at equal width.</summary>
    Grid,

    /// <summary>Only the focused column is rendered, at the full content width.</summary>
    Maximized,

    /// <summary>
    /// Columns are stacked vertically: the focused column is expanded and the
    /// rest collapse to a single title line.
    /// </summary>
    Stacked
}

/// <summary>
/// One column's computed slot within a <see cref="BoardLayoutDecision"/>.
/// </summary>
/// <param name="Width">The column's width in cells.</param>
/// <param name="Height">The column's height in cells.</param>
/// <param name="Expanded">
/// Whether the column renders its full panel, as opposed to a collapsed
/// title line.
/// </param>
internal readonly record struct BoardColumnLayout(int Width, int Height, bool Expanded);

/// <summary>
/// The layout chosen for one frame: which <see cref="BoardLayoutKind"/> applies
/// and the slot each column renders into, indexed the same as the board's
/// column list.
/// </summary>
internal sealed class BoardLayoutDecision
{
    /// <summary>
    /// The arrangement this decision resolved to.
    /// </summary>
    public required BoardLayoutKind Kind { get; init; }

    /// <summary>
    /// Each column's computed slot, indexed the same as the board's column list.
    /// </summary>
    public required IReadOnlyList<BoardColumnLayout> Columns { get; init; }
}

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
    /// <see cref="BoardLayoutKind.Stacked"/>.
    /// </summary>
    private const int CollapsedStackedHeight = 1;

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

    private static IReadOnlyList<BoardColumnLayout> BuildStacked(
        int width, int height, int columnCount, int focused)
    {
        var columns = new BoardColumnLayout[columnCount];
        var collapsedTotal = CollapsedStackedHeight * (columnCount - 1);
        var expandedHeight = Math.Max(1, height - collapsedTotal);

        for (var i = 0; i < columnCount; i++)
        {
            columns[i] = i == focused
                ? new BoardColumnLayout(width, expandedHeight, Expanded: true)
                : new BoardColumnLayout(width, CollapsedStackedHeight, Expanded: false);
        }

        return columns;
    }
}
