namespace Mocha;

/// <summary>
/// Fires a callback after a delay. Cancellable and restartable.
/// All methods except <see cref="DisposeAsync"/> must be called under the caller's lock.
/// </summary>
internal sealed class DelayedAction(TimeSpan delay, TimeProvider timeProvider, Func<ValueTask> onElapsed)
    : IAsyncDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public void Start()
    {
        Cancel();
        _cts = new CancellationTokenSource();
        _runningTask = RunAsync(_cts.Token);
    }

    public void Cancel()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Cancel();

        if (_runningTask is not null)
        {
            await _runningTask;
            _runningTask = null;
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, timeProvider, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await onElapsed();
    }
}
