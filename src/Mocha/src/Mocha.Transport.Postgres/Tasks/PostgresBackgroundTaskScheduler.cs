namespace Mocha.Transport.Postgres.Tasks;

/// <summary>
/// Manages the lifecycle of <see cref="PostgresBackgroundTask"/> instances,
/// starting them all together and stopping them during shutdown.
/// </summary>
internal sealed class PostgresBackgroundTaskScheduler : IAsyncDisposable
{
    private readonly List<PostgresBackgroundTask> _tasks = [];

    /// <summary>
    /// Registers a background task.
    /// </summary>
    /// <param name="task">The task to register.</param>
    public void Add(PostgresBackgroundTask task)
    {
        _tasks.Add(task);
    }

    /// <summary>
    /// Starts all registered background tasks.
    /// </summary>
    public void Start()
    {
        foreach (var task in _tasks)
        {
            task.Start();
        }
    }

    /// <summary>
    /// Stops all registered background tasks and clears the list.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var task in _tasks)
        {
            await task.StopAsync();
        }

        _tasks.Clear();
    }
}
