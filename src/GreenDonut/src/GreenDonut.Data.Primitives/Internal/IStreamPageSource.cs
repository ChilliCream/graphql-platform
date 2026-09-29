namespace GreenDonut.Data.Internal;

/// <summary>
/// The buffering, flag resolution, and disposal behind a <see cref="StreamPage{T}"/>, abstracted
/// from whether the page items are the buffered elements themselves or a projection of them.
/// </summary>
/// <typeparam name="TValue">
/// The type of the page items.
/// </typeparam>
internal interface IStreamPageSource<TValue> : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether this source has finished streaming.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets the total count of items in the dataset, or null if it is not yet known.
    /// </summary>
    int? TotalCount { get; }

    /// <summary>
    /// Gets the requested page size, or null if it is unknown.
    /// </summary>
    int? RequestedSize { get; }

    /// <summary>
    /// Gets the number of entries buffered so far.
    /// </summary>
    int BufferedCount { get; }

    /// <summary>
    /// Enumerates the buffered entries, reading ahead from the source as needed.
    /// </summary>
    IAsyncEnumerable<PageEntry<TValue>> GetEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates the buffered items, reading ahead from the source as needed.
    /// </summary>
    IAsyncEnumerable<TValue> GetValuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the total count, reading ahead only as far as needed.
    /// </summary>
    ValueTask<int?> TotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves whether there is a next page, reading ahead only as far as needed.
    /// </summary>
    ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves whether there is a previous page, reading ahead only as far as needed.
    /// </summary>
    ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads from the source until one entry is buffered or the source completes. This is the
    /// mandatory step between construction and handing a page to a consumer.
    /// </summary>
    ValueTask PrimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the already buffered entry at the given index, without reading ahead.
    /// </summary>
    PageEntry<TValue> GetBufferedEntry(int index);
}
