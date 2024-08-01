namespace HotChocolate.Subscriptions.Postgres;

internal sealed class AsyncTaskDispatcher : IAsyncDisposable
{
    private readonly TaskCompletionSource<bool> _initialize = new();
    private readonly AsyncAutoResetEvent _event = new();
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly Func<CancellationToken, Task> _handler;
    private readonly ContinuousTask _eventProcessorTask;

    private bool _disposed;
    private bool _initialized;

    public AsyncTaskDispatcher(Func<CancellationToken, Task> handler)
    {
        _handler = handler;
        _eventProcessorTask = new ContinuousTask(EventHandler, TimeProvider.System);
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AsyncTaskDispatcher));
        }

        if (_initialized)
        {
            return;
        }

        await _sync.WaitAsync(cancellationToken);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AsyncTaskDispatcher));
        }

        if (_initialized)
        {
            return;
        }

        try
        {
            _event.Set();
            await _initialize.Task
                .WaitAsync(cancellationToken) // in case the caller cancels the operation
                .WaitAsync(_eventProcessorTask.Completion); // in case we dispose this instance

            _initialized = true;
        }
        finally
        {
            _sync.Release();
        }
    }

    public void Dispatch()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AsyncTaskDispatcher));
        }

        _event.Set();
    }

    private async Task EventHandler(CancellationToken cancellationToken)
    {
        await _event.WaitAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // we cannot be sure if the handler await the cancellation token and therefore we
        // chain a WaitAsync to sure we cancel on dispose
        await _handler(cancellationToken).WaitAsync(cancellationToken);
        _initialize.TrySetResult(true);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _eventProcessorTask.DisposeAsync();
        _event.Dispose();

        _disposed = true;
    }
}
