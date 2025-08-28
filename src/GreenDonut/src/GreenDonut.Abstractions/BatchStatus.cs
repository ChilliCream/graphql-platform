namespace GreenDonut;

/// <summary>
/// Defines the lifecycle states of a batch within the scheduler/dispatcher pipeline.
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// <para>
    /// The batch has received new work items (keys, requests, etc.)
    /// since the last scheduler observation.
    /// </para>
    /// <para>
    /// This state is typically set when the first item is enqueued into
    /// an empty batch or when new items are added after the previous
    /// scheduler turn, indicating that the batch may be eligible for
    /// dispatch in the near future.
    /// </para>
    /// </summary>
    Enqueued = 1,

    /// <summary>
    /// <para>
    /// The batch has been observed ("touched") by the scheduler or dispatcher
    /// in the current scheduling turn.
    /// </para>
    /// <para>
    /// This state is used as a hint that the batch has been considered for
    /// dispatch but is being given an additional opportunity to accumulate
    /// more items before execution. Typically, a touched batch will be
    /// dispatched on the next turn if it receives no new items.
    /// </para>
    /// </summary>
    Touched = 2
}
