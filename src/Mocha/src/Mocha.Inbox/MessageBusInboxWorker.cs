using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mocha.Threading;

namespace Mocha.Inbox;

/// <summary>
/// A hosted service that manages the lifecycle of the inbox cleanup processor,
/// running the cleanup loop as a continuous background task.
/// </summary>
/// <param name="inboxOptions">The inbox configuration options including retention period and cleanup interval.</param>
/// <param name="provider">The service provider used to resolve scoped services.</param>
/// <param name="timeProvider">The time provider used for scheduling cleanup operations.</param>
/// <param name="cleanupLogger">The logger for the cleanup processor.</param>
/// <param name="logger">The logger for the worker lifecycle.</param>
internal sealed class MessageBusInboxWorker(
    IOptions<InboxOptions> inboxOptions,
    IServiceProvider provider,
    TimeProvider timeProvider,
    ILogger<InboxCleanupProcessor> cleanupLogger,
    ILogger<MessageBusInboxWorker> logger) : IHostedService
{
    private ContinuousTask? _task;

    /// <summary>
    /// Starts the inbox cleanup background task.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when startup should be aborted.</param>
    /// <returns>A completed task once the background loop has been initiated.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the worker is already running.</exception>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_task is not null)
        {
            throw new InvalidOperationException("The worker is already running.");
        }

        logger.InboxWorkerStarting();

        _task = new ContinuousTask(ProcessAsync);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the inbox cleanup background task and waits for it to complete gracefully.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when shutdown should be forced.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.InboxWorkerStopping();

        if (_task is null)
        {
            return;
        }

        await _task.DisposeAsync();
        _task = null;
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        var processor = new InboxCleanupProcessor(inboxOptions, timeProvider, provider, cleanupLogger);

        await processor.ProcessAsync(stoppingToken);
    }
}
