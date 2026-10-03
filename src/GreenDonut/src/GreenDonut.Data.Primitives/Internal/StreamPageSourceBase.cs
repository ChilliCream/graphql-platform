using System.Runtime.CompilerServices;

namespace GreenDonut.Data.Internal;

/// <summary>
/// Provides the shared item enumeration for an <see cref="IStreamPageSource{TValue}"/>, projecting
/// it from the entries once.
/// </summary>
/// <typeparam name="TValue">
/// The type of the page items.
/// </typeparam>
internal abstract class StreamPageSourceBase<TValue> : IStreamPageSource<TValue>
{
    /// <inheritdoc />
    public abstract bool IsCompleted { get; }

    /// <inheritdoc />
    public abstract int? TotalCount { get; }

    /// <inheritdoc />
    public abstract int? RequestedSize { get; }

    /// <inheritdoc />
    public abstract int BufferedCount { get; }

    /// <inheritdoc />
    public abstract IAsyncEnumerable<PageEntry<TValue>> GetEntriesAsync(
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public IAsyncEnumerable<TValue> GetValuesAsync(CancellationToken cancellationToken = default)
        => GetValuesCore(cancellationToken);

    /// <inheritdoc />
    public abstract ValueTask<int?> TotalCountAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract ValueTask PrimeAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract PageEntry<TValue> GetBufferedEntry(int index);

    /// <inheritdoc />
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
