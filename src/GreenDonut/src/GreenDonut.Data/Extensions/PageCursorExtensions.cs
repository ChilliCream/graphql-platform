namespace GreenDonut.Data;

/// <summary>
/// Extensions for creating cursors from the boundaries of a page.
/// </summary>
public static class PageCursorExtensions
{
    /// <summary>
    /// Creates a cursor for the first item of the page.
    /// </summary>
    public static string? CreateStartCursor<T>(this Page<T> page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.FirstIndex is not null ? page.CreateCursor(page.FirstIndex.Value) : null;
    }

    /// <summary>
    /// Creates a cursor for the last item of the page.
    /// </summary>
    public static string? CreateEndCursor<T>(this Page<T> page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.LastIndex is not null ? page.CreateCursor(page.LastIndex.Value) : null;
    }
}
