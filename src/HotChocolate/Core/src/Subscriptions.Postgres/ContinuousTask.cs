namespace HotChocolate.Subscriptions.Postgres;

internal sealed class ContinuousTask : IAsyncDisposable
{
    private readonly TimeSpan _waitOnFailure = TimeSpan.FromSeconds(1);

    private readonly CancellationTokenSource _completion = new();
    private readonly Func<CancellationToken, Task> _handler;
    private readonly TimeProvider _timeProvider;
    private readonly Task _task;
    private bool _disposed;

    public ContinuousTask(Func<CancellationToken, Task> handler, TimeProvider timeProvider)
    {
        _handler = handler;
        _timeProvider = timeProvider;

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
#if NET8_0_OR_GREATER
                    await Task.Delay(_waitOnFailure, _timeProvider, _completion.Token);
#else
                    await _timeProvider.Delay(_waitOnFailure, _completion.Token);
#endif
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
