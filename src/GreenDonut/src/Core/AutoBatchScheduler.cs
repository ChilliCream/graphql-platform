using System.Threading.Tasks;

namespace GreenDonut;

/// <summary>
/// Defines a batch dispatcher that immediately dispatches batch jobs.
/// </summary>
public class AutoBatchScheduler : IBatchScheduler
{
    /// <summary>
    /// Schedules a new job to the dispatcher that is immediately executed.
    /// </summary>
    /// <param name="job">
    /// The job that is being scheduled.
    /// </param>
    public void Schedule(BatchJob job)
        => BeginDispatch(job);

    private static void BeginDispatch(BatchJob job)
        => Task.Factory.StartNew(
            async () => await job.DispatchAsync().ConfigureAwait(false),
            default,
            TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default);

    /// <summary>
    /// Gets the default instance if the <see cref="AutoBatchScheduler"/>.
    /// </summary>
    public static AutoBatchScheduler Default { get; } = new();
}