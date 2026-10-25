namespace HotChocolate.Fetching;

/// <summary>
/// Configuration options for the batch dispatcher.
/// </summary>
public struct BatchDispatcherOptions
{
    private const long DefaultMaxBatchWaitTimeUs = 50_000;
    private const long DefaultBatchSettleTimeUs = 250;
    private const long MaxBatchSettleTimeUs = 10_000_000;

    private long _maxBatchWaitTimeUs;
    private bool _maxBatchWaitTimeSet;
    private long _batchSettleTimeUs;
    private bool _batchSettleTimeSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDispatcherOptions"/> class.
    /// </summary>
    public BatchDispatcherOptions()
    {
    }

    /// <summary>
    /// Gets or sets the maximum wait time in microseconds before a batch is forcefully dispatched.
    /// Set this value to 0 to disable forced dispatch based on batch age.
    /// </summary>
    public long MaxBatchWaitTimeUs
    {
        readonly get => _maxBatchWaitTimeSet ? _maxBatchWaitTimeUs : DefaultMaxBatchWaitTimeUs;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(
                value,
                other: 0,
                nameof(MaxBatchWaitTimeUs));
            _maxBatchWaitTimeUs = value;
            _maxBatchWaitTimeSet = true;
        }
    }

    /// <summary>
    /// Gets or sets the minimum time in microseconds that a batch must go without receiving
    /// new items before it becomes eligible for dispatch. Larger values improve batching
    /// efficiency under concurrent load at the cost of added latency per dispatch.
    /// Set this value to 0 to make a batch eligible as soon as it is observed unchanged
    /// between two evaluations. The valid range is 0 to 10,000,000 microseconds.
    /// </summary>
    public long BatchSettleTimeUs
    {
        readonly get => _batchSettleTimeSet ? _batchSettleTimeUs : DefaultBatchSettleTimeUs;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(
                value,
                other: 0,
                nameof(BatchSettleTimeUs));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(
                value,
                other: MaxBatchSettleTimeUs,
                nameof(BatchSettleTimeUs));
            _batchSettleTimeUs = value;
            _batchSettleTimeSet = true;
        }
    }
}
