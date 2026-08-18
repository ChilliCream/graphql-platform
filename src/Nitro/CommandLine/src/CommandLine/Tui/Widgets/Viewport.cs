namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// Tracks the visible window over a list of a given length: the scroll
/// offset, how many rows are visible at once, and which range is currently
/// on screen. Does not render anything itself.
/// </summary>
internal sealed class Viewport
{
    public Viewport(int totalCount, int windowHeight)
    {
        Update(totalCount, windowHeight);
    }

    /// <summary>
    /// The total number of rows in the underlying list.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// The number of rows visible at once.
    /// </summary>
    public int WindowHeight { get; private set; }

    /// <summary>
    /// The index of the first visible row.
    /// </summary>
    public int Offset { get; private set; }

    /// <summary>
    /// The number of rows above the visible window.
    /// </summary>
    public int HiddenAbove => Offset;

    /// <summary>
    /// The number of rows below the visible window.
    /// </summary>
    public int HiddenBelow => Math.Max(0, TotalCount - (Offset + WindowHeight));

    /// <summary>
    /// Updates the total row count and window height, clamping the offset so
    /// it still points at a valid window.
    /// </summary>
    public void Update(int totalCount, int windowHeight)
    {
        TotalCount = Math.Max(0, totalCount);
        WindowHeight = Math.Max(0, windowHeight);
        ClampOffset();
    }

    /// <summary>
    /// Scrolls the window, if needed, so that <paramref name="index"/> is
    /// visible. Out-of-range indexes are clamped to the list bounds.
    /// </summary>
    public void EnsureVisible(int index)
    {
        if (TotalCount == 0 || WindowHeight == 0)
        {
            Offset = 0;
            return;
        }

        var clampedIndex = Math.Clamp(index, 0, TotalCount - 1);

        if (clampedIndex < Offset)
        {
            Offset = clampedIndex;
        }
        else if (clampedIndex > Offset + WindowHeight - 1)
        {
            Offset = clampedIndex - WindowHeight + 1;
        }

        ClampOffset();
    }

    /// <summary>
    /// Returns the currently visible range as a start index and row count.
    /// </summary>
    public (int Start, int Count) Slice()
    {
        if (TotalCount == 0 || WindowHeight == 0)
        {
            return (0, 0);
        }

        return (Offset, Math.Min(WindowHeight, TotalCount - Offset));
    }

    private void ClampOffset()
    {
        var maxOffset = Math.Max(0, TotalCount - WindowHeight);
        Offset = Math.Clamp(Offset, 0, maxOffset);
    }
}
