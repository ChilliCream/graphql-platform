namespace GreenDonut;

/// <summary>
/// Defines a batch dispatcher that immediately dispatches batch jobs.
/// </summary>
public class AutoBatchScheduler : IBatchScheduler
{
    /// <summary>
    /// Schedules a new batch that is immediately executed.
    /// </summary>
    /// <param name="batch">
    /// The batch.
    /// </param>
    public void Schedule(Batch batch)
        => BeginDispatch(batch);

    private static void BeginDispatch(Batch batch)
        => Task.Run(async () =>
        {
            try
            {
                await batch.DispatchAsync().ConfigureAwait(false);
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
