namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents a dedicated options accessor to read the configuration
/// of the query execution engine streaming features.
/// </summary>
public interface IStreamOptionsAccessor
{
    /// <summary>
    /// The number of items that can be prefetched from
    /// the data source and buffered while streaming.
    /// </summary>
    int StreamBufferSize { get; }
}
