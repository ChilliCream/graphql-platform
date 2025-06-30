namespace GreenDonut.Data.Cursors;

/// <summary>
/// Represents pagination information parsed from a cursor, including offset, current page, and total count.
/// </summary>
public readonly ref struct CursorPageInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CursorPageInfo"/> struct.
    /// </summary>
    /// <param name="nullsFirst">Determines whether null values should appear first in the sort order.</param>
    public CursorPageInfo(bool nullsFirst)
    {
        NullsFirst = nullsFirst;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorPageInfo"/> struct.
    /// </summary>
    /// <param name="nullsFirst">Determines whether null values should appear first in the sort order.</param>
    /// <param name="offset">Offset indicating the number of items/pages skipped.</param>
    /// <param name="pageIndex">The zero-based index of the current page.</param>
    /// <param name="totalCount">Total number of items available in the dataset.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if an offset greater than zero is specified with a totalCount of zero.
    /// </exception>
    public CursorPageInfo(bool nullsFirst, int offset, int pageIndex, int totalCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pageIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        if (offset > 0 && totalCount == 0)
        {
            throw new ArgumentException(
                "The total count must be greater than zero if an offset is set.",
                nameof(totalCount));
        }

        Offset = offset;
        PageIndex = pageIndex;
        TotalCount = totalCount;
        NullsFirst = nullsFirst;
    }

    /// <summary>
    /// Gets the offset relative to the current cursor position.
    /// Positive for forward offset, negative for backward offset, zero if no offset.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the zero-based index of the current page within the paginated dataset.
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// Gets the total count of items in the entire dataset, if known.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Determines whether null values should appear first in the sort order.
    /// </summary>
    public bool NullsFirst { get; }

    /// <summary>
    /// Deconstructs the <see cref="CursorPageInfo"/> into individual components.
    /// </summary>
    /// <param name="nullsFirst">The nulls first order if no valid data or <c>false</c> and nulls last order for <c>true</c>.</param>
    /// <param name="offset">The offset, or <c>null</c> if no valid data.</param>
    /// <param name="pageIndex">The page number, or <c>null</c> if no valid data.</param>
    /// <param name="totalCount">The total count, or <c>null</c> if no valid data.</param>
    public void Deconstruct(out bool nullsFirst, out int? offset, out int? pageIndex, out int? totalCount)
    {
        if (TotalCount == 0)
        {
            offset = null;
            pageIndex = null;
            totalCount = null;
            nullsFirst = false;
            return;
        }

        offset = Offset;
        pageIndex = PageIndex;
        totalCount = TotalCount;
        nullsFirst = NullsFirst;
    }
}
