using Microsoft.Extensions.Hosting;
using Mocha.Threading;

namespace Mocha.Scheduling;

/// <summary>
/// A hosted service that manages the lifecycle of the Postgres scheduled message dispatcher,
/// running the processing loop as a continuous background task.
/// </summary>
/// <param name="dispatcher">The dispatcher that performs the scheduled message dispatch loop.</param>
internal sealed class ScheduledMessageWorker(ScheduledMessageDispatcher dispatcher) : IHostedService
{
    private readonly object _lock = new();
    private ContinuousTask? _task;

    /// <summary>
    /// Starts the scheduled message processing background task. This call is idempotent: invoking it
    /// again while the worker is already running is a no-op that returns without starting a second loop.
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

            _task = new ContinuousTask(dispatcher.ProcessAsync);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the scheduled message processing background task and waits for it to complete gracefully.
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
    }
}
