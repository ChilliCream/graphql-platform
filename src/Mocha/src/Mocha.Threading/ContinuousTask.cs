using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Mocha.Threading;

/// <summary>
/// Runs a delegate continuously in the background, restarting it after each completion or failure.
/// </summary>
/// <remarks>
/// The handler is invoked in a loop until the task is disposed. Unhandled exceptions trigger an
/// exponential backoff (100 ms base, 10 s cap) before the next invocation, preventing tight
/// failure loops. Disposal signals the <see cref="Completion"/> token and awaits graceful
/// shutdown. Because the work is async, the task is not started with
/// <c>TaskCreationOptions.LongRunning</c>.
/// </remarks>
public sealed class ContinuousTask : IAsyncDisposable
{
    private readonly ExponentialBackoff _backoff = new(
        int.MaxValue,
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromSeconds(10));

    private readonly CancellationTokenSource _completion = new();
    private readonly Func<CancellationToken, Task> _handler;
    private readonly Task _task;
    private bool _disposed;

    /// <summary>
    /// Creates a new continuous task that immediately begins executing the specified handler.
    /// </summary>
    /// <param name="handler">
    /// The asynchronous delegate to invoke repeatedly. Receives a <see cref="CancellationToken"/>
    /// that is signaled when the task is disposed.
    /// </param>
    public ContinuousTask(Func<CancellationToken, Task> handler)
    {
        _handler = handler;

        // We do not use Task.Factory.StartNew here because RunContinuously is an async method and
        // the LongRunning flag only works until the first await.
        _task = RunContinuously();
    }

    /// <summary>
    /// Gets a token that is signaled when the continuous task has been disposed and the processing loop should stop.
    /// </summary>
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
            catch (OperationCanceledException) when (_completion.IsCancellationRequested) { }
            catch (Exception ex)
            {
                Activity.Current?.AddException(ex);
                // App.Log.UnexpectedExceptionInProcessingWorker(ex);

                if (!_completion.IsCancellationRequested)
                {
                    await _backoff.WaitAsync(_completion.Token);
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (!_completion.IsCancellationRequested)
        {
#if NET8_0_OR_GREATER
            await _completion.CancelAsync();
#else
            _completion.Cancel();
#endif
        }

        // Wait for the background loop to finish before disposing the CTS.
        // This prevents ObjectDisposedException if RunContinuously is still
        // accessing _completion.Token when we dispose it.
        try
        {
            await _task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }

        _completion.Dispose();

        _disposed = true;
    }
}

/// <summary>
/// Provides high-performance source-generated log methods for the threading infrastructure.
/// </summary>
public static partial class Logs
{
    [LoggerMessage(
        0,
        LogLevel.Error,
        "Unexpected exception in processing worker.",
        EventName = "UnexpectedExceptionInProcessingWorker")]
    public static partial void UnexpectedExceptionInProcessingWorker(this ILogger logger, Exception exception);
}
