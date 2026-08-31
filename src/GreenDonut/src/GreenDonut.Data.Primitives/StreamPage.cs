using System.Runtime.CompilerServices;

namespace GreenDonut.Data;

/// <summary>
/// Represents a one-shot stream of a result set page.
/// </summary>
/// <typeparam name="T">
/// The type of the items.
/// </typeparam>
public sealed class StreamPage<T> : IAsyncEnumerable<StreamPageEdge<T>>
{
    private readonly IAsyncEnumerable<T> _items;
    private readonly PagingArguments _arguments;
    private readonly Func<EdgeEntry<T>, string> _createCursor;
    private readonly TaskCompletionSource<StreamPageCompletion> _completionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _enumerationStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamPage{T}"/> class.
    /// </summary>
    /// <param name="items">
    /// The items in the page window, including one additional item when one is available.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments that bound the page stream.
    /// </param>
    /// <param name="createCursor">
    /// Creates a cursor for an item.
    /// </param>
    /// <param name="totalCount">
    /// The total count of items in the dataset, or <see langword="null"/> when it is unknown.
    /// </param>
    public StreamPage(
        IAsyncEnumerable<T> items,
        PagingArguments arguments,
        Func<T, string> createCursor,
        int? totalCount = null)
        : this(items, arguments, entry => createCursor(entry.Node), totalCount)
    {
        ArgumentNullException.ThrowIfNull(createCursor);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamPage{T}"/> class.
    /// </summary>
    /// <param name="items">
    /// The items in the page window, including one additional item when one is available.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments that bound the page stream.
    /// </param>
    /// <param name="createCursor">
    /// Creates a cursor for an item and its positional metadata.
    /// </param>
    /// <param name="totalCount">
    /// The total count of items in the dataset, or <see langword="null"/> when it is unknown.
    /// </param>
    public StreamPage(
        IAsyncEnumerable<T> items,
        PagingArguments arguments,
        Func<EdgeEntry<T>, string> createCursor,
        int? totalCount = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(createCursor);
        ArgumentOutOfRangeException.ThrowIfNegative(arguments.First ?? 0);

        _items = items;
        _arguments = arguments;
        _createCursor = createCursor;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets a one-shot stream of the page items.
    /// </summary>
    public IAsyncEnumerable<T> Items => GetItemsAsync();

    /// <summary>
    /// Gets the total count of items in the dataset.
    /// </summary>
    public int? TotalCount { get; }

    /// <summary>
    /// Gets the completion signal for the streamed page.
    /// </summary>
    public Task<StreamPageCompletion> Completion => _completionSource.Task;

    /// <inheritdoc />
    public IAsyncEnumerator<StreamPageEdge<T>> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _enumerationStarted, 1) != 0)
        {
            throw new InvalidOperationException("A streamed page can only be enumerated once.");
        }

        return EnumerateAsync(cancellationToken).GetAsyncEnumerator();
    }

    private async IAsyncEnumerable<T> GetItemsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var edge in this.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return edge.Node;
        }
    }

    private async IAsyncEnumerable<StreamPageEdge<T>> EnumerateAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var completed = false;

        try
        {
            string? startCursor = null;
            string? endCursor = null;
            var hasNextPage = false;
            var itemIndex = 0;
            var requestedSize = _arguments.First;

            await using (var enumerator = _items.GetAsyncEnumerator(cancellationToken))
            {
                while (requestedSize is null || itemIndex < requestedSize)
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    var cursor = _createCursor(new EdgeEntry<T>(enumerator.Current, 0, 0, 0));
                    startCursor ??= cursor;
                    endCursor = cursor;
                    itemIndex++;

                    yield return new StreamPageEdge<T>(enumerator.Current, cursor);
                }

                if (requestedSize is not null)
                {
                    hasNextPage = await enumerator.MoveNextAsync();
                }
            }

            _completionSource.TrySetResult(
                new StreamPageCompletion(hasNextPage, _arguments.After is not null, startCursor, endCursor));
            completed = true;
        }
        finally
        {
            if (!completed)
            {
                _completionSource.TrySetCanceled();
            }
        }
    }
}
