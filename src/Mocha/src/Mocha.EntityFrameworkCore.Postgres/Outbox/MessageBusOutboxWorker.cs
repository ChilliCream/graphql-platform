using Microsoft.Extensions.Hosting;
using Mocha.Threading;

namespace Mocha.Outbox;

/// <summary>
/// A hosted service that manages the lifecycle of the Postgres outbox processor,
/// running the processing loop as a continuous background task.
/// </summary>
/// <param name="processor">The outbox processor that performs the message dispatch loop.</param>
internal sealed class PostgresMessageBusOutboxWorker(PostgresOutboxProcessor processor) : IHostedService
{
    private readonly object _lock = new();
    private ContinuousTask? _task;
    private Task _processing = Task.CompletedTask;

    /// <summary>
    /// Starts the outbox processing background task. This call is idempotent: invoking it again
    /// while the worker is already running is a no-op that returns without starting a second loop.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when startup should be aborted.</param>
    /// <returns>A completed task once the background loop has been initiated.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_task is not null)
            {
                return Task.CompletedTask;
            }

            _task = new ContinuousTask(ProcessAsync);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the outbox processing background task and waits until the messages that were being dispatched
    /// are either removed from the outbox or released.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when shutdown should be forced.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ContinuousTask? task;

        lock (_lock)
        {
            task = _task;
            _task = null;
        }

        if (task is null)
        {
            return;
        }

        await task.DisposeAsync();

        // The continuous task stops waiting once it is cancelled, while the processor still removes the
        // messages a transport has already accepted. The wait ends early when the shutdown is forced.
        await Volatile.Read(ref _processing)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private Task ProcessAsync(CancellationToken cancellationToken)
    {
        var processing = processor.ProcessAsync(cancellationToken);
        Volatile.Write(ref _processing, processing);
        return processing;
    }
}
