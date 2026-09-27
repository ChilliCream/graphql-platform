namespace GreenDonut.Data;

/// <summary>
/// Extensions for creating cursors from the boundaries of a streaming page.
/// </summary>
public static class StreamPageCursorExtensions
{
    /// <summary>
    /// Creates a cursor for the first item of the page, without reading ahead.
    /// </summary>
    /// <param name="page">
    /// The page to create the cursor for.
    /// </param>
    /// <returns>
    /// The cursor of the first item, or null if the page is empty.
    /// </returns>
    public static string? CreateStartCursor<T>(this StreamPage<T> page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return page.BufferedCount > 0 ? page.CreateCursor(page.GetBufferedEntry(0)) : null;
    }

    /// <summary>
    /// Creates a cursor for the last item of the page, reading ahead until the source completes.
    /// </summary>
    /// <param name="page">
    /// The page to create the cursor for.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// The cursor of the last item, or null if the page is empty.
    /// </returns>
    public static async ValueTask<string?> CreateEndCursorAsync<T>(
        this StreamPage<T> page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        await page.DrainAsync(cancellationToken).ConfigureAwait(false);

        return page.BufferedCount > 0
            ? page.CreateCursor(page.GetBufferedEntry(page.BufferedCount - 1))
            : null;
    }
}
