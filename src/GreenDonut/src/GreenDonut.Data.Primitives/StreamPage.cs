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
    private Exception? _completionException;
    private bool _sourceFailed;

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

        if (arguments.Last is not null || arguments.Before is not null)
        {
            throw ThrowHelper.StreamPage_BackwardPaginationNotSupported(nameof(arguments));
        }

        _items = items;
        _arguments = arguments;
        _createCursor = createCursor;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets a one-shot stream of the page items.
    /// </summary>
    public IAsyncEnumerable<T> Items => new ItemsEnumerable(this);

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
            throw ThrowHelper.StreamPage_EnumerationCanOnlyOccurOnce();
        }

        return new Enumerator(this, cancellationToken);
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
            var fetchCount = 0;
            var itemIndex = 0;
            var requestedSize = _arguments.First;

            var enumerator = GetSourceAsyncEnumerator(cancellationToken);
            try
            {
                while (requestedSize is null || itemIndex < requestedSize)
                {
                    if (!await MoveNextAsync(enumerator, cancellationToken))
                    {
                        break;
                    }

                    fetchCount++;
                    var cursor = CreateCursor(
                        new EdgeEntry<T>(GetCurrent(enumerator, cancellationToken), 0, 0, 0),
                        cancellationToken);
                    startCursor ??= cursor;
                    endCursor = cursor;
                    itemIndex++;

                    yield return new StreamPageEdge<T>(GetCurrent(enumerator, cancellationToken), cursor);
                }

                if (requestedSize is not null)
                {
                    hasNextPage = await MoveNextAsync(enumerator, cancellationToken);

                    if (hasNextPage)
                    {
                        fetchCount++;
                    }
                }
            }
            finally
            {
                try
                {
                    await DisposeAsync(enumerator, cancellationToken, !_sourceFailed);
                }
                catch when (_sourceFailed)
                {
                }
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
                if (_completionException is { } exception)
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

    private IAsyncEnumerator<T> GetSourceAsyncEnumerator(CancellationToken cancellationToken)
    {
        try
        {
            return _items.GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception exception)
        {
            RecordSourceException(exception, cancellationToken);
            throw;
        }
    }

    private ValueTask<bool> MoveNextAsync(
        IAsyncEnumerator<T> enumerator,
        CancellationToken cancellationToken)
    {
        try
        {
            var operation = enumerator.MoveNextAsync();
            return operation.IsCompletedSuccessfully
                ? operation
                : AwaitMoveNextAsync(operation, cancellationToken);
        }
        catch (Exception exception)
        {
            RecordSourceException(exception, cancellationToken);
            throw;
        }
    }

    private async ValueTask<bool> AwaitMoveNextAsync(
        ValueTask<bool> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            RecordSourceException(exception, cancellationToken);
            throw;
        }
    }

    private T GetCurrent(IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken)
    {
        try
        {
            return enumerator.Current;
        }
        catch (Exception exception)
        {
            RecordSourceException(exception, cancellationToken);
            throw;
        }
    }

    private ValueTask DisposeAsync(
        IAsyncEnumerator<T> enumerator,
        CancellationToken cancellationToken,
        bool recordException)
    {
        try
        {
            var operation = enumerator.DisposeAsync();
            return operation.IsCompletedSuccessfully
                ? operation
                : AwaitDisposeAsync(operation, cancellationToken, recordException);
        }
        catch (Exception exception)
        {
            if (recordException)
            {
                RecordCompletionException(exception, cancellationToken);
            }

            throw;
        }
    }

    private async ValueTask AwaitDisposeAsync(
        ValueTask operation,
        CancellationToken cancellationToken,
        bool recordException)
    {
        try
        {
            await operation.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (recordException)
            {
                RecordCompletionException(exception, cancellationToken);
            }

            throw;
        }
    }

    private string CreateCursor(EdgeEntry<T> entry, CancellationToken cancellationToken)
    {
        try
        {
            return _createCursor(entry);
        }
        catch (Exception exception)
        {
            RecordSourceException(exception, cancellationToken);
            throw;
        }
    }

    private void RecordSourceException(Exception exception, CancellationToken cancellationToken)
    {
        _sourceFailed = true;
        RecordCompletionException(exception, cancellationToken);
    }

    private void RecordCompletionException(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not OperationCanceledException operationCanceledException
            || operationCanceledException.CancellationToken != cancellationToken
            ||
            !cancellationToken.IsCancellationRequested)
        {
            _completionException ??= exception;
        }
    }

    private sealed class Enumerator(StreamPage<T> page, CancellationToken cancellationToken)
        : IAsyncEnumerator<StreamPageEdge<T>>
    {
        private IAsyncEnumerator<StreamPageEdge<T>>? _enumerator;
        private int _state;

        public StreamPageEdge<T> Current => _enumerator!.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) is 2)
            {
                return ValueTask.FromResult(false);
            }

            _enumerator ??= page.EnumerateAsync(cancellationToken).GetAsyncEnumerator();
            return _enumerator.MoveNextAsync();
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) is 0)
            {
                page._completionSource.TrySetCanceled();
                return ValueTask.CompletedTask;
            }

            return _enumerator is { } enumerator
                ? enumerator.DisposeAsync()
                : ValueTask.CompletedTask;
        }
    }

    private sealed class ItemsEnumerable(StreamPage<T> page) : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new ItemsEnumerator(page.GetAsyncEnumerator(cancellationToken));
    }

    private sealed class ItemsEnumerator(IAsyncEnumerator<StreamPageEdge<T>> enumerator)
        : IAsyncEnumerator<T>
    {
        public T Current => enumerator.Current.Node;

        public ValueTask<bool> MoveNextAsync()
            => enumerator.MoveNextAsync();

        public ValueTask DisposeAsync()
            => enumerator.DisposeAsync();
    }
}
