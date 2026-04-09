using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Mocha.Threading;

namespace Mocha.Scheduling;

/// <summary>
/// A hosted service that manages the lifecycle of the SQL Server scheduled message dispatcher,
/// opening a dedicated SQL Server connection and running the processing loop as a continuous background task.
/// </summary>
/// <param name="options">The scheduled message options containing the SQL Server connection string.</param>
/// <param name="dispatcher">The dispatcher that performs the scheduled message dispatch loop.</param>
internal sealed class SqlServerScheduledMessageWorker(
    SqlServerScheduledMessageOptions options,
    SqlServerScheduledMessageDispatcher dispatcher)
    : IHostedService
{
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
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(stoppingToken);

        await dispatcher.ProcessAsync(connection, stoppingToken);
    }
}
