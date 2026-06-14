using Microsoft.Extensions.Hosting;
using Mocha.Threading;
using Npgsql;

namespace Mocha.Scheduling;

/// <summary>
/// A hosted service that manages the lifecycle of the Postgres scheduled message dispatcher,
/// opening a dedicated Npgsql connection and running the processing loop as a continuous background task.
/// </summary>
/// <param name="options">The scheduled message options containing the Postgres connection string.</param>
/// <param name="dispatcher">The dispatcher that performs the scheduled message dispatch loop.</param>
internal sealed class ScheduledMessageWorker(
    PostgresScheduledMessageOptions options,
    ScheduledMessageDispatcher dispatcher)
    : IHostedService
{
    private readonly object _lock = new();
    private NpgsqlDataSource? _dataSource;
    private ContinuousTask? _task;

    /// <summary>
    /// Starts the scheduled message processing background task.
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

            _dataSource = NpgsqlDataSource.Create(options.ConnectionString);
            _task = new ContinuousTask(ProcessAsync);
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

        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
            _dataSource = null;
        }
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _dataSource!.OpenConnectionAsync(stoppingToken);

        await dispatcher.ProcessAsync(connection, stoppingToken);
    }
}
