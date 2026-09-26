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
    /// <inheritdoc />
    public bool IsCompleted => buffer.IsCompleted;

    /// <inheritdoc />
    public int? TotalCount => buffer.TotalCount;

    /// <inheritdoc />
    public int? RequestedSize => buffer.RequestedSize;

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
    public ValueTask DisposeAsync() => buffer.DisposeAsync();

    private async IAsyncEnumerable<TValue> EnumerateAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var element in buffer.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return valueSelector(element);
        }
    }

    private async IAsyncEnumerable<PageEntry<TValue>> EnumerateEntriesCore(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var entries = buffer.EnumerateEntriesAsync(cancellationToken);

        await foreach (var entry in entries.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return new PageEntry<TValue>(valueSelector(entry.Item), entry.Index);
        }
    }
}
