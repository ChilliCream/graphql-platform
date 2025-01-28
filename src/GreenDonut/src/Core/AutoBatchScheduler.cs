namespace GreenDonut;

/// <summary>
/// Defines a batch dispatcher that immediately dispatches batch jobs.
/// </summary>
public class AutoBatchScheduler : IBatchScheduler
{
    /// <summary>
    /// Schedules a new job to the dispatcher that is immediately executed.
    /// </summary>
    /// <param name="dispatch">
    /// The job that is being scheduled.
    /// </param>
    public void Schedule(Func<ValueTask> dispatch)
        => BeginDispatch(dispatch);

    private static void BeginDispatch(Func<ValueTask> dispatch)
        => Task.Run(async () =>
        {
            try
            {
                await dispatch().ConfigureAwait(false);
            }
            catch
            {
                // we do ignore any potential exceptions here
            }
        });

    /// <summary>
    /// Gets the default instance if the <see cref="AutoBatchScheduler"/>.
    /// </summary>
    public static AutoBatchScheduler Default { get; } = new();
}
