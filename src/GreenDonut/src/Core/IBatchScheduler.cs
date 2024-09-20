namespace GreenDonut;

/// <summary>
/// The batch scheduler is used by the DataLoader to defer the data fetching
/// work to a batch dispatcher that will execute the batches.
/// </summary>
public interface IBatchScheduler
{
    /// <summary>
    /// Schedules work.
    /// </summary>
    /// <param name="dispatch">
    /// A delegate that represents the work.
    /// </param>
    void Schedule(Func<ValueTask> dispatch);
}
