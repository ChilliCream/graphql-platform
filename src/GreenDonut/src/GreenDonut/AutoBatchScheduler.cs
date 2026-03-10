namespace GreenDonut;

/// <summary>
/// A simple batch scheduler implementation that immediately dispatches batches
/// without coordination or batching optimization. This scheduler prioritizes
/// low latency over batching efficiency by executing each batch as soon as
/// it is scheduled.
/// </summary>
public class AutoBatchScheduler : IBatchScheduler
{
    /// <summary>
    /// Schedules a batch for immediate execution. The batch is dispatched
    /// asynchronously on a background thread without waiting for additional
    /// keys or coordination with other batches.
    /// </summary>
    /// <param name="batch">
    /// The batch to be immediately dispatched for execution.
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
    /// Gets the default shared instance of the <see cref="AutoBatchScheduler"/>.
    /// </summary>
    public static AutoBatchScheduler Default { get; } = new();
}
