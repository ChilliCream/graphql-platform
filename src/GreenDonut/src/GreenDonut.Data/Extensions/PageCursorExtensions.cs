namespace GreenDonut.Data;

/// <summary>
/// Extensions for creating cursors from the boundaries of a page.
/// </summary>
public static class PageCursorExtensions
{
    /// <summary>
    /// Creates a cursor for the first item of the page.
    /// When the page knows its index and the total count, the cursor is a relative cursor
    /// with an offset of 0, so that paging on with it keeps the page numbers.
    /// </summary>
    public static string? CreateStartCursor<T>(this Page<T> page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.First is not null ? CreateBoundaryCursor(page, page.First.Value) : null;
    }

    /// <summary>
    /// Creates a cursor for the last item of the page.
    /// When the page knows its index and the total count, the cursor is a relative cursor
    /// with an offset of 0, so that paging on with it keeps the page numbers.
    /// </summary>
    public static string? CreateEndCursor<T>(this Page<T> page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.Last is not null ? CreateBoundaryCursor(page, page.Last.Value) : null;
    }

    private static string CreateBoundaryCursor<T>(Page<T> page, PageEntry<T> entry)
        => page.Index is not null && page.TotalCount is not null
            ? page.CreateCursor(entry, 0)
            : page.CreateCursor(entry);
}
