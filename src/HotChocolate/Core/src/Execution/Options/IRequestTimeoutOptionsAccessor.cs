namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents a dedicated options accessor to read the configured query
/// execution timeout.
/// </summary>
public interface IRequestTimeoutOptionsAccessor
{
    /// <summary>
    /// Gets maximum allowed execution time of a query. The default value
    /// is <c>30</c> seconds. The minimum allowed value is <c>100</c>
    /// milliseconds.
    /// </summary>
    TimeSpan ExecutionTimeout { get; }
}
