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
        var completionState = new CompletionState();

        try
        {
            string? startCursor = null;
            string? endCursor = null;
            var hasNextPage = false;
            var fetchCount = 0;
            var itemIndex = 0;
            var requestedSize = _arguments.First;

            var enumerator = GetAsyncEnumerator(cancellationToken, completionState);
            try
            {
                while (requestedSize is null || itemIndex < requestedSize)
                {
                    if (!await MoveNextAsync(enumerator, completionState))
                    {
                        break;
                    }

                    fetchCount++;
                    var cursor = CreateCursor(new EdgeEntry<T>(enumerator.Current, 0, 0, 0), completionState);
                    startCursor ??= cursor;
                    endCursor = cursor;
                    itemIndex++;

                    yield return new StreamPageEdge<T>(enumerator.Current, cursor);
                }

                if (requestedSize is not null)
                {
                    hasNextPage = await MoveNextAsync(enumerator, completionState);

                    if (hasNextPage)
                    {
                        fetchCount++;
                    }
                }
            }
            finally
            {
                await DisposeAsync(enumerator, completionState);
            }

            _completionSource.TrySetResult(
                new StreamPageCompletion(
                    hasNextPage,
                    _arguments.After is not null && fetchCount > 0,
                    startCursor,
                    endCursor));
            completed = true;
        }
        finally
        {
            if (!completed)
            {
                if (completionState.Exception is { } exception)
                {
                    _completionSource.TrySetException(exception);
                }
                else
                {
                    _completionSource.TrySetCanceled();
                }
            }
        }
    }

    private IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken,
        CompletionState completionState)
    {
        try
        {
            return _items.GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            completionState.Exception = exception;
            throw;
        }
    }

    private static async ValueTask<bool> MoveNextAsync(
        IAsyncEnumerator<T> enumerator,
        CompletionState completionState)
    {
        try
        {
            return await enumerator.MoveNextAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            completionState.Exception = exception;
            throw;
        }
    }

    private static async ValueTask DisposeAsync(IAsyncEnumerator<T> enumerator, CompletionState completionState)
    {
        try
        {
            await enumerator.DisposeAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            completionState.Exception = exception;
            throw;
        }
    }

    private string CreateCursor(EdgeEntry<T> entry, CompletionState completionState)
    {
        try
        {
            return _createCursor(entry);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            completionState.Exception = exception;
            throw;
        }
    }

    private sealed class CompletionState
    {
        public Exception? Exception { get; set; }
    }
}
