namespace GreenDonut.Data.Internal;

/// <summary>
/// The buffering, flag resolution, and disposal behind a <see cref="StreamPage{T}"/>, abstracted
/// from whether the page items are the buffered elements themselves or a projection of them.
/// </summary>
/// <typeparam name="TValue">
/// The type of the page items.
/// </typeparam>
internal interface IStreamPageSource<TValue> : IAsyncEnumerable<TValue>, IAsyncDisposable
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
    /// Enumerates the buffered entries, reading ahead from the source as needed.
    /// </summary>
    IAsyncEnumerable<PageEntry<TValue>> EnumerateEntriesAsync(CancellationToken cancellationToken = default);

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
}
