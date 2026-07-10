namespace Mocha;

/// <summary>
/// Configuration for batch message collection behavior.
/// </summary>
public sealed class BatchOptions
{
    /// <summary>
    /// Gets or sets the maximum number of messages per batch. Default: 100.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time window before flushing a partial batch. Default: 1 second.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum number of batches that can be processed concurrently.
    /// Higher values improve throughput when batch processing is slow relative to message
    /// arrival rate, at the cost of losing ordering guarantees between batches. Default: 1.
    /// </summary>
    public int MaxConcurrentBatches { get; set; } = 1;

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxBatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(BatchTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxConcurrentBatches, 1);
    }
}
