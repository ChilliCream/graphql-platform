namespace HotChocolate.Subscriptions.Postgres;

internal sealed class ContinuousTask : IAsyncDisposable
{
    private const int _waitOnFailureinMs = 1000;

    private readonly CancellationTokenSource _completion = new();
    private readonly Func<CancellationToken, Task> _handler;
    private readonly Task _task;
    private bool _disposed;

    public ContinuousTask(Func<CancellationToken, Task> handler)
    {
        _handler = handler;

        // We do not use Task.Factory.StartNew here because RunContinuously is an async method and
        // the LongRunning flag only works until the first await.
        _task = RunContinuously();
    }

    public CancellationToken Completion => _completion.Token;

    private async Task RunContinuously()
    {
        while (!_completion.IsCancellationRequested)
        {
            try
            {
                // we don't know if the handler awaits the cancellation token and therefore we
                // chain a WaitAsync to sure we cancel on dispose
                await _handler(_completion.Token).WaitAsync(_completion.Token);

                // we yield so that even a sync handler can be executed in the background without
                // a never ending loop
                await Task.Yield();
            }
            catch
            {
                if (!_completion.IsCancellationRequested)
                {
                    await Task.Delay(_waitOnFailureinMs, _completion.Token);
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if(_disposed)
        {
            return;
        }

        if(!_completion.IsCancellationRequested)
        {
#if NET8_0_OR_GREATER
            await _completion.CancelAsync();
#else
            _completion.Cancel();
#endif
        }

        _completion.Dispose();
        _disposed = true;

        await ValueTask.CompletedTask;
    }
}
