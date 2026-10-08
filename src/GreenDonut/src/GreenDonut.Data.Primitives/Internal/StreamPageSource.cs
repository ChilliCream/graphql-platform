using System.Runtime.CompilerServices;

namespace GreenDonut.Data.Internal;

/// <summary>
/// The buffering, flag resolution, and disposal behind a <see cref="StreamPage{T}"/>, abstracted
/// from whether the page items are the buffered elements themselves or a projection of them.
/// </summary>
/// <typeparam name="TValue">
/// The type of the page items.
/// </typeparam>
internal abstract class StreamPageSource<TValue> : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether this source has finished streaming.
    /// </summary>
    public abstract bool IsCompleted { get; }

    /// <summary>
    /// Gets the total count of items in the dataset, or null if it is not yet known.
    /// </summary>
    public abstract int? TotalCount { get; }

    /// <summary>
    /// Gets the requested page size, or null if it is unknown.
    /// </summary>
    public abstract int? RequestedSize { get; }

    /// <summary>
    /// Gets the number of entries buffered so far.
    /// </summary>
    public abstract int BufferedCount { get; }

    /// <summary>
    /// Enumerates the buffered entries, reading ahead from the source as needed.
    /// </summary>
    public abstract IAsyncEnumerable<PageEntry<TValue>> GetEntriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates the buffered items, reading ahead from the source as needed.
    /// </summary>
    public IAsyncEnumerable<TValue> GetValuesAsync(CancellationToken cancellationToken = default)
        => GetValuesCore(cancellationToken);

    /// <summary>
    /// Resolves the total count, reading ahead only as far as needed.
    /// </summary>
    public abstract ValueTask<int?> TotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves whether there is a next page, reading ahead only as far as needed.
    /// </summary>
    public abstract ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves whether there is a previous page, reading ahead only as far as needed.
    /// </summary>
    public abstract ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads from the source until one entry is buffered or the source completes. This is the
    /// mandatory step between construction and handing a page to a consumer.
    /// </summary>
    public abstract ValueTask PrimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the already buffered entry at the given index, without reading ahead.
    /// </summary>
    public abstract PageEntry<TValue> GetBufferedEntry(int index);

    /// <summary>
    /// Releases the resources held by this source.
    /// </summary>
    public abstract ValueTask DisposeAsync();

    private async IAsyncEnumerable<TValue> GetValuesCore(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var entries = GetEntriesAsync(cancellationToken);

        await foreach (var entry in entries.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return entry.Item;
        }
    }
}
