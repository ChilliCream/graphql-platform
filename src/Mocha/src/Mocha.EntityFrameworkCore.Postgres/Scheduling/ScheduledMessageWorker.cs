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

            // The loop captures its own data source rather than reading the field, so that
            // StopAsync (or a concurrent restart) can clear and dispose the field without
            // affecting an already-running loop.
            var dataSource = NpgsqlDataSource.Create(options.ConnectionString);
            _task = new ContinuousTask(token => ProcessAsync(dataSource, token));
            _dataSource = dataSource;
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
        NpgsqlDataSource? dataSource;

        lock (_lock)
        {
            task = _task;
            dataSource = _dataSource;
            _task = null;
            _dataSource = null;
        }

        if (task is null)
        {
            return;
        }

        // Dispose the data source even if the loop fails to shut down cleanly, so the
        // underlying connection pool is always released.
        try
        {
            await task.DisposeAsync();
        }
        finally
        {
            if (dataSource is not null)
            {
                await dataSource.DisposeAsync();
            }
        }
    }

    private async Task ProcessAsync(NpgsqlDataSource dataSource, CancellationToken stoppingToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(stoppingToken);

        await dispatcher.ProcessAsync(connection, stoppingToken);
    }
}
