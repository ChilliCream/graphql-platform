using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// Base class for periodic background tasks in the PostgreSQL messaging transport.
/// Provides a consistent lifecycle (Start/StopAsync) and error handling pattern
/// using <see cref="PeriodicTimer"/> for scheduling.
/// </summary>
internal abstract class PostgresBackgroundTask(
    PostgresConnectionManager connectionManager,
    IReadOnlyPostgresSchemaOptions schemaOptions,
    ILogger logger)
{
    private Task? _runningTask;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Gets the connection manager used to open database connections.
    /// </summary>
    protected PostgresConnectionManager ConnectionManager => connectionManager;

    /// <summary>
    /// Gets the schema and table naming options.
    /// </summary>
    protected IReadOnlyPostgresSchemaOptions SchemaOptions => schemaOptions;

    /// <summary>
    /// Gets the logger for this task.
    /// </summary>
    protected ILogger Logger => logger;

    /// <summary>
    /// Gets the interval between task executions.
    /// </summary>
    protected abstract TimeSpan Interval { get; }

    /// <summary>
    /// Executes the task's work. Called once per interval.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    protected abstract Task ExecuteAsync(CancellationToken ct);

    /// <summary>
    /// Starts the background task loop.
    /// </summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _runningTask = RunAsync(_cts.Token);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            try
            {
                await ExecuteAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.BackgroundTaskFailed(ex, GetType().Name);
            }
        }
    }

    /// <summary>
    /// Stops the background task and waits for it to complete.
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_runningTask is not null)
        {
            try
            {
                await _runningTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                // Proceed with cleanup
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        _cts?.Dispose();
        _cts = null;
        _runningTask = null;
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Error, "Background task {TaskName} failed.")]
    public static partial void BackgroundTaskFailed(this ILogger logger, Exception exception, string taskName);
}
