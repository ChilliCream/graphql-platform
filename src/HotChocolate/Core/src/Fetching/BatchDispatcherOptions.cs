namespace HotChocolate.Fetching;

/// <summary>
/// Configuration options for the batch dispatcher.
/// </summary>
public struct BatchDispatcherOptions
{
    private int _maxParallelBatches = 4;
    private long _maxBatchWaitTimeUs = 50_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDispatcherOptions"/> class.
    /// </summary>
    public BatchDispatcherOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether batches can be dispatched in parallel.
    /// </summary>
    public bool EnableParallelBatches { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of batches that can be dispatched in parallel.
    /// </summary>
    public int MaxParallelBatches
    {
        readonly get => _maxParallelBatches;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 2, nameof(MaxParallelBatches));
            _maxParallelBatches = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum wait time in microseconds before a batch is forcefully dispatched.
    /// Disable max batch wait time by setting this value to 0.
    /// </summary>
    public long MaxBatchWaitTimeUs
    {
        readonly get => _maxBatchWaitTimeUs;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(MaxBatchWaitTimeUs));
            _maxBatchWaitTimeUs = value;
        }
    }
}
