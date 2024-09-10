using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Options;

/// <summary>
/// Defines the options for the result buffers.
/// </summary>
public sealed class ResultBufferOptions
{
    private int _maximumAllowedCapacity = ResultPoolDefaults.MaximumAllowedCapacity;
    private int _bucketSize = ResultPoolDefaults.BucketSize;
    private int _maximumRetained = ResultPoolDefaults.MaximumRetained;

    /// <summary>
    /// Defines the maximum number of pooled result objects that are retained in a bucket.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The maximum retained must be greater than or equal to 16.
    /// </exception>
    public int MaximumRetained
    {
        get => _maximumRetained;
        set
        {
            if(value <= 16)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The maximum retained must be greater than or equal to 16.");
            }

            _maximumRetained = value;
        }
    }

    /// <summary>
    /// Defines the bucket size for pooled result objects.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The bucket size must be greater than or equal to 8.
    /// </exception>
    public int BucketSize
    {
        get
        {
            return _bucketSize;
        }
        set
        {
            if(value <= 8)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The bucket size must be greater than or equal to 8.");
            }

            _bucketSize = value;
        }
    }

    /// <summary>
    /// Defines the maximum allowed fields or list entries of a pooled object.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The maximum allowed capacity must be greater than or equal to 16.
    /// </exception>
    public int MaximumAllowedCapacity
    {
        get => _maximumAllowedCapacity;
        set
        {
            if(value <= 16)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The maximum allowed capacity must be greater than or equal to 16.");
            }

            _maximumAllowedCapacity = value;
        }
    }
}
