namespace GreenDonut;

/// <summary>
/// The batch scheduler is used by DataLoaders to coordinate efficient batch execution
/// of data fetching operations. The scheduler defers individual data requests into
/// batches that are processed by a batch dispatcher, optimizing throughput while
/// maintaining low latency.
/// </summary>
public interface IBatchScheduler
{
    /// <summary>
    /// <para>
    /// Schedules a batch for execution. The batch will be queued and processed
    /// by the batch dispatcher using an intelligent coordination strategy that
    /// prioritizes batches based on their modification timestamp to ensure
    /// optimal batching efficiency.
    /// </para>
    /// </summary>
    /// <param name="batch">
    /// The batch containing one or more data loading keys that should be
    /// scheduled for coordinated execution.
    /// </param>
    void Schedule(Batch batch);
}
