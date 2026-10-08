namespace GreenDonut.Data;

/// <summary>
/// Extensions for creating cursors from the boundaries of a streaming page.
/// </summary>
public static class StreamPageCursorExtensions
{
    /// <summary>
    /// Creates a cursor for the first item of the page, reading ahead only until the first item
    /// has arrived.
    /// </summary>
    /// <param name="page">
    /// The page to create the cursor for.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// The cursor of the first item, or null if the page is empty.
    /// </returns>
    public static async ValueTask<string?> CreateStartCursorAsync<T>(
        this StreamPage<T> page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        await using var entries = page.GetEntriesAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        return await entries.MoveNextAsync().ConfigureAwait(false)
            ? page.CreateCursor(entries.Current)
            : null;
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

        PageEntry<T>? lastEntry = null;

        await foreach (var entry in page.GetEntriesAsync(cancellationToken).ConfigureAwait(false))
        {
            lastEntry = entry;
        }

        return lastEntry is null ? null : page.CreateCursor(lastEntry.Value);
    }
}
