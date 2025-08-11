namespace GreenDonut;

/// <summary>
/// The batch scheduler is used by the DataLoader to defer the data fetching
/// work to a batch dispatcher that will execute the batches.
/// </summary>
public interface IBatchScheduler
{
    /// <summary>
    /// Schedules a batch.
    /// </summary>
    /// <param name="batch">
    /// The batch that was scheduled for execution.
    /// </param>
    void Schedule(Batch batch);
}
