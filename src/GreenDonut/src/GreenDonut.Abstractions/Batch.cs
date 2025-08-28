namespace GreenDonut;

/// <summary>
/// <para>
/// Represents a unit of work to be executed by a batch dispatcher.
/// A batch groups together multiple keys or requests that can be
/// resolved in a single execution step.
/// </para>
/// <para>
/// This abstract base class defines the minimal contract required
/// for schedulers and dispatchers to interact with batches without
/// depending on their concrete key or value types.
/// </para>
/// </summary>
public abstract class Batch
{
    /// <summary>
    /// <para>
    /// Gets the number of items currently contained in this batch.
    /// </para>
    /// <para>
    /// This reflects the current size of the batch and can be used
    /// to decide whether to dispatch early (e.g., when reaching a
    /// maximum size threshold).
    /// </para>
    /// </summary>
    public abstract int Size { get; }

    /// <summary>
    /// <para>
    /// Gets the current status of this batch.
    /// </para>
    /// <para>
    /// The status indicates whether the batch is newly created, has
    /// been observed ("touched") by the scheduler in the current
    /// turn/epoch, or is ready for dispatch.
    /// </para>
    /// </summary>
    public abstract BatchStatus Status { get; }

    /// <summary>
    /// Gets a high-resolution timestamp from representing the last time an item was added to this batch.
    /// This value is used for recency checks in scheduling decisions.
    /// </summary>
    public abstract long ModifiedTimestamp { get; }

    /// <summary>
    /// <para>
    /// Marks the batch as "touched" by the scheduler.
    /// </para>
    /// <para>
    /// This is typically called when the scheduler observes the batch
    /// during a scheduling turn, indicating that it has been considered
    /// for dispatch. A touched batch may be given a short grace period
    /// or additional turn to gather more items before being dispatched.
    /// </para>
    /// </summary>
    /// <returns>
    /// <c>true</c> if it did not change since it last was touched.
    /// </returns>
    public abstract bool Touch();

    /// <summary>
    /// Dispatch this batch.
    /// </summary>
    public abstract Task DispatchAsync();
}
