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
    ScheduledMessageDispatcher dispatcher) : IHostedService
{
    private NpgsqlDataSource? _dataSource;
    private ContinuousTask? _task;

    /// <summary>
    /// Starts the scheduled message processing background task.
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

        _dataSource = NpgsqlDataSource.Create(options.ConnectionString);
        _task = new ContinuousTask(ProcessAsync);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the scheduled message processing background task and waits for it to complete gracefully.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when shutdown should be forced.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_task is null)
        {
            return;
        }

        await _task.DisposeAsync();
        _task = null;

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
