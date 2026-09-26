using System.Runtime.CompilerServices;

namespace GreenDonut.Data.Internal;

/// <summary>
/// Projects a <see cref="StreamPageBuffer{TElement}"/> of source rows into page items of a
/// different type, applying the projection to each row as it streams from the shared buffer.
/// </summary>
/// <typeparam name="TElement">
/// The type of the buffered source rows.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the page items.
/// </typeparam>
internal sealed class ElementProjectingSource<TElement, TValue>(
    StreamPageBuffer<TElement> buffer,
    Func<TElement, TValue> valueSelector) : IStreamPageSource<TValue>
{
    private readonly List<TValue> _values = [];

    /// <inheritdoc />
    public bool IsCompleted => buffer.IsCompleted;

    /// <inheritdoc />
    public int? TotalCount => buffer.TotalCount;

    /// <inheritdoc />
    public int? RequestedSize => buffer.RequestedSize;

    /// <inheritdoc />
    public int BufferedCount => buffer.BufferedCount;

    /// <inheritdoc />
    public IAsyncEnumerator<TValue> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => EnumerateAsync(cancellationToken).GetAsyncEnumerator();

    /// <inheritdoc />
    public IAsyncEnumerable<PageEntry<TValue>> EnumerateEntriesAsync(CancellationToken cancellationToken = default)
        => EnumerateEntriesCore(cancellationToken);

    /// <inheritdoc />
    public ValueTask<int?> TotalCountAsync(CancellationToken cancellationToken = default)
        => buffer.TotalCountAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default)
        => buffer.HasNextPageAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default)
        => buffer.HasPreviousPageAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask PrimeAsync(CancellationToken cancellationToken = default)
        => buffer.PrimeAsync(cancellationToken);

    /// <inheritdoc />
    public PageEntry<TValue> GetBufferedEntry(int index) => new(Get(index), index);

    /// <inheritdoc />
    public ValueTask DrainAsync(CancellationToken cancellationToken = default)
        => buffer.DrainAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => buffer.DisposeAsync();

    // Projects and caches source rows into page items on demand, so a row is only ever run
    // through the value selector once no matter how many times the page is enumerated.
    private TValue Get(int index)
    {
        while (_values.Count <= index)
        {
            _values.Add(valueSelector(buffer[_values.Count]));
        }

        return _values[index];
    }

    private async IAsyncEnumerable<TValue> EnumerateAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var entries = buffer.EnumerateEntriesAsync(cancellationToken);

        await foreach (var entry in entries.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return Get(entry.Index);
        }
    }

    private async IAsyncEnumerable<PageEntry<TValue>> EnumerateEntriesCore(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var entries = buffer.EnumerateEntriesAsync(cancellationToken);

        await foreach (var entry in entries.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return new PageEntry<TValue>(Get(entry.Index), entry.Index);
        }
    }
}
