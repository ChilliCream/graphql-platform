using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mocha.Threading;
using Npgsql;

namespace Mocha.Outbox;

/// <summary>
/// A hosted service that manages the lifecycle of the Postgres outbox processor,
/// opening a dedicated Npgsql connection and running the processing loop as a continuous background task.
/// </summary>
/// <param name="options">The outbox options containing the Postgres connection string.</param>
/// <param name="processor">The outbox processor that performs the message dispatch loop.</param>
internal sealed class PostgresMessageBusOutboxWorker(
    PostgresMessageOutboxOptions options,
    PostgresOutboxProcessor processor) : IHostedService
{
    private readonly NpgsqlDataSource _dataSource = NpgsqlDataSource.Create(options.ConnectionString);

    private ContinuousTask? _task;

    /// <summary>
    /// Starts the outbox processing background task.
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

        _task = new ContinuousTask(ProcessAsync);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the outbox processing background task and waits for it to complete gracefully.
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
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);

        await processor.ProcessAsync(connection, stoppingToken);
    }
}
