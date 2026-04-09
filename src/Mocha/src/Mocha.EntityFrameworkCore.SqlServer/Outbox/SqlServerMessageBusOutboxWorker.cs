using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Mocha.Threading;

namespace Mocha.Outbox;

/// <summary>
/// A hosted service that manages the lifecycle of the SQL Server outbox processor,
/// opening a dedicated SQL Server connection and running the processing loop as a continuous background task.
/// </summary>
/// <param name="options">The outbox options containing the SQL Server connection string.</param>
/// <param name="processor">The outbox processor that performs the message dispatch loop.</param>
internal sealed class SqlServerMessageBusOutboxWorker(
    SqlServerMessageOutboxOptions options,
    SqlServerOutboxProcessor processor) : IHostedService
{
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
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(stoppingToken);

        await processor.ProcessAsync(connection, stoppingToken);
    }
}
