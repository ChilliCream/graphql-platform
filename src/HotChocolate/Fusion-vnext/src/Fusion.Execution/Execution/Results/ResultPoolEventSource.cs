using System.Diagnostics.Tracing;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// EventSource for HotChocolate Fusion result pooling telemetry.
/// </summary>
[EventSource(Name = "HotChocolate-Fusion-Result-Pools")]
public sealed class ResultPoolEventSource : EventSource
{
    /// <summary>
    /// The singleton instance of the ResultPoolEventSource.
    /// </summary>
    public static readonly ResultPoolEventSource Log = new();

    // Prevent external instantiation
    private ResultPoolEventSource() { }

    #region Pool Session Events

    /// <summary>
    /// Logged when a new pool session is created (scoped service instantiation).
    /// </summary>
    [Event(1, Level = EventLevel.Informational, Keywords = Keywords.Session)]
    public void SessionCreated()
    {
        if (IsEnabled(EventLevel.Informational, Keywords.Session))
        {
            WriteEvent(1);
        }
    }

    /// <summary>
    /// Logged when a pool session is disposed/reset.
    /// </summary>
    [Event(2, Level = EventLevel.Informational, Keywords = Keywords.Session)]
    public void SessionDisposed(int totalRents, int batchesUsed)
    {
        if (IsEnabled(EventLevel.Informational, Keywords.Session))
        {
            WriteEvent(2, totalRents, batchesUsed);
        }
    }

    #endregion

    #region Batch Events

    /// <summary>
    /// Logged when a batch is exhausted and a new one needs to be allocated.
    /// </summary>
    /// <param name="poolType">The type of pool (e.g., "ObjectResult", "LeafFieldResult").</param>
    /// <param name="batchIndex">The index of the exhausted batch within the session.</param>
    [Event(10, Level = EventLevel.Verbose, Keywords = Keywords.Batch)]
    public void BatchExhausted(string poolType, int batchIndex)
    {
        if (IsEnabled(EventLevel.Verbose, Keywords.Batch))
        {
            WriteEvent(10, poolType, batchIndex);
        }
    }

    /// <summary>
    /// Logged when a new batch is allocated from the object pool.
    /// </summary>
    /// <param name="poolType">The type of pool.</param>
    /// <param name="batchIndex">The index of the new batch within the session.</param>
    [Event(11, Level = EventLevel.Verbose, Keywords = Keywords.Batch)]
    public void BatchAllocated(string poolType, int batchIndex)
    {
        if (IsEnabled(EventLevel.Verbose, Keywords.Batch))
        {
            WriteEvent(11, poolType, batchIndex);
        }
    }

    /// <summary>
    /// Logged when a batch is reset and returned to the pool.
    /// </summary>
    /// <param name="poolType">The type of pool.</param>
    /// <param name="objectsRented">Number of objects that were rented from this batch.</param>
    /// <param name="objectsRecreated">Number of objects that had to be recreated during reset.</param>
    [Event(12, Level = EventLevel.Verbose, Keywords = Keywords.Batch)]
    public void BatchReset(string poolType, int objectsRented, int objectsRecreated)
    {
        if (IsEnabled(EventLevel.Verbose, Keywords.Batch))
        {
            WriteEvent(12, poolType, objectsRented, objectsRecreated);
        }
    }

    #endregion

    #region Object Rental Events

    /// <summary>
    /// Logged when an object is successfully rented from a batch.
    /// Only enabled at LogAlways level to minimize performance impact.
    /// </summary>
    /// <param name="poolType">The type of pool.</param>
    /// <param name="objectIndex">The index within the batch.</param>
    [Event(20, Level = EventLevel.LogAlways, Keywords = Keywords.Rental)]
    public void ObjectRented(string poolType, int objectIndex)
    {
        if (IsEnabled(EventLevel.LogAlways, Keywords.Rental))
        {
            WriteEvent(20, poolType, objectIndex);
        }
    }

    /// <summary>
    /// Logged when an object new pooled object is being created.
    /// </summary>
    /// <param name="poolType">The type of pool.</param>
    /// <param name="reason">Reason for recreation.</param>
    [Event(21, Level = EventLevel.Warning, Keywords = Keywords.Rental)]
    public void ObjectCreated(string poolType, ObjectRecreationReason reason)
    {
        if (IsEnabled(EventLevel.Warning, Keywords.Rental))
        {
            WriteEvent(21, poolType, (int)reason);
        }
    }

    /// <summary>
    /// Reasons why an object was recreated instead of reused.
    /// </summary>
    public enum ObjectRecreationReason
    {
        /// <summary>Object was recreated when creating a new batch.</summary>
        NewBatch = 1,

        /// <summary>Object reset failed and had to be recreated.</summary>
        ResetFailed = 2
    }

    #endregion

    #region Performance Counters

    /// <summary>
    /// Logged to provide batch utilization metrics.
    /// </summary>
    /// <param name="poolType">The type of pool.</param>
    /// <param name="batchSize">The configured batch size.</param>
    /// <param name="utilizationPercentage">Percentage of batch that was actually used.</param>
    [Event(30, Level = EventLevel.Informational, Keywords = Keywords.Performance)]
    public void BatchUtilization(string poolType, int batchSize, int utilizationPercentage)
    {
        if (IsEnabled(EventLevel.Informational, Keywords.Performance))
        {
            WriteEvent(30, poolType, batchSize, utilizationPercentage);
        }
    }

    #endregion

    #region Error Events

    /// <summary>
    /// Logged when pool corruption is detected (fresh batch cannot provide objects).
    /// </summary>
    /// <param name="poolType">The type of pool.</param>
    [Event(40, Level = EventLevel.Critical, Keywords = Keywords.Error)]
    public void PoolCorruption(string poolType)
    {
        if (IsEnabled(EventLevel.Critical, Keywords.Error))
        {
            WriteEvent(40, poolType);
        }
    }

    /// <summary>
    /// Logged when capacity limits are exceeded and objects are recreated.
    /// </summary>
    /// <param name="poolType">The type of pool.</param>
    /// <param name="currentCapacity">The current capacity that exceeded limits.</param>
    /// <param name="maxAllowed">The maximum allowed capacity.</param>
    [Event(41, Level = EventLevel.Warning, Keywords = Keywords.Error)]
    public void CapacityExceeded(string poolType, int currentCapacity, int maxAllowed)
    {
        if (IsEnabled(EventLevel.Warning, Keywords.Error))
        {
            WriteEvent(41, poolType, currentCapacity, maxAllowed);
        }
    }

    #endregion

    /// <summary>
    /// Keywords for categorizing events and enabling selective filtering.
    /// </summary>
    public static class Keywords
    {
        /// <summary>Pool session lifecycle events.</summary>
        public const EventKeywords Session = (EventKeywords)0x1;

        /// <summary>Batch allocation and management events.</summary>
        public const EventKeywords Batch = (EventKeywords)0x2;

        /// <summary>Individual object rental events.</summary>
        public const EventKeywords Rental = (EventKeywords)0x4;

        /// <summary>Performance and utilization metrics.</summary>
        public const EventKeywords Performance = (EventKeywords)0x8;

        /// <summary>Error and warning conditions.</summary>
        public const EventKeywords Error = (EventKeywords)0x10;
    }
}
