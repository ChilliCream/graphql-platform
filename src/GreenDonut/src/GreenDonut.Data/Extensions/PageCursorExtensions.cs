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
        return page.First is not null ? page.CreateCursor(page.First.Value) : null;
    }

    /// <summary>
    /// Creates a cursor for the last item of the page.
    /// </summary>
    public static string? CreateEndCursor<T>(this Page<T> page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.Last is not null ? page.CreateCursor(page.Last.Value) : null;
    }
}
