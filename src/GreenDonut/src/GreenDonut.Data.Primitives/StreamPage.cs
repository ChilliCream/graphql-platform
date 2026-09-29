using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

/// <summary>
/// Represents a page of a result set whose items and page info stream from an underlying source.
/// </summary>
/// <typeparam name="T">
/// The type of the items.
/// </typeparam>
/// <remarks>
/// The source is read once; enumerating the page more than once, or enumerating it with more
/// than one enumerator at a time, replays or shares the same buffered rows. This type has no
/// cross-thread safety, the same stance as <c>DbContext</c>.
/// </remarks>
public abstract class StreamPage<T> : IAsyncEnumerable<T>, IAsyncDisposable
{
    private readonly IStreamPageSource<T> _source;

    private protected StreamPage(IStreamPageSource<T> source, int? index)
    {
        _source = source;
        Index = index;
    }

    /// <summary>
    /// Gets the index number of this page.
    /// </summary>
    public int? Index { get; }

    /// <summary>
    /// Gets the requested page size.
    /// This value can be null if the page size is unknown.
    /// </summary>
    internal int? RequestedSize => _source.RequestedSize;

    /// <summary>
    /// Gets the total count of items in the dataset.
    /// This value is null until the count has arrived from the source.
    /// </summary>
    internal int? TotalCount => _source.TotalCount;

    /// <summary>
    /// Gets the enumerator for the items of this page.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the enumeration.
    /// </param>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => _source.GetValuesAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

    /// <summary>
    /// Enumerates the entries of this page, bundling each item with its position.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the enumeration.
    /// </param>
    public IAsyncEnumerable<PageEntry<T>> GetEntriesAsync(CancellationToken cancellationToken = default)
        => _source.GetEntriesAsync(cancellationToken);

    /// <summary>
    /// Gets the total count of items in the dataset, reading ahead only as far as needed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// Returns the total count, or null if it was not requested.
    /// </returns>
    public ValueTask<int?> TotalCountAsync(CancellationToken cancellationToken = default)
        => _source.TotalCountAsync(cancellationToken);

    /// <summary>
    /// Gets a value indicating whether there is a next page, reading ahead only as far as needed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    public ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default)
        => _source.HasNextPageAsync(cancellationToken);

    /// <summary>
    /// Gets a value indicating whether there is a previous page, reading ahead only as far as
    /// needed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    public ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default)
        => _source.HasPreviousPageAsync(cancellationToken);

    /// <summary>
    /// Creates a cursor for an entry of this page.
    /// </summary>
    /// <param name="entry">
    /// The entry for which a cursor shall be created.
    /// </param>
    /// <returns>
    /// Returns a cursor for the entry.
    /// </returns>
    public string CreateCursor(PageEntry<T> entry) => CreateCursor(entry.Index, 0, 0, 0);

    /// <summary>
    /// Creates a relative cursor for an entry of this page.
    /// </summary>
    /// <param name="entry">
    /// The entry for which a cursor shall be created.
    /// </param>
    /// <param name="offset">
    /// The offset relative to the current cursor position.
    /// </param>
    /// <returns>
    /// Returns a cursor for the entry.
    /// </returns>
    public string CreateCursor(PageEntry<T> entry, int offset)
    {
        if (Index is null || TotalCount is null)
        {
            throw ThrowHelper.StreamPage_RelativeCursorsNotAllowed();
        }

        return CreateCursor(entry.Index, offset, Index.Value, TotalCount.Value);
    }

    /// <summary>
    /// An empty, completed page.
    /// </summary>
    public static StreamPage<T> Empty => ValueCursorStreamPage<T>.Empty;

    /// <summary>
    /// Releases the resources held by this page. Disposing before completion stops reading from
    /// the source; disposing a completed page is a no-op, and its buffered items stay readable
    /// either way.
    /// </summary>
    public ValueTask DisposeAsync() => _source.DisposeAsync();

    protected abstract string CreateCursor(int index, int offset, int pageIndex, int totalCount);
}
