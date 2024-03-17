namespace GreenDonut;

/// <summary>
/// The batch scheduler is used by the DataLoader to defer the data fetching
/// work to a batch dispatcher that will execute the batches.
/// </summary>
public interface IBatchScheduler
{
    /// <summary>
    /// Schedules the work that has to be executed to fetch the data.
    /// </summary>
    /// <param name="job">
    /// The work that has to be executed to fetch the data.
    /// </param>
    void Schedule(BatchJob job);
}