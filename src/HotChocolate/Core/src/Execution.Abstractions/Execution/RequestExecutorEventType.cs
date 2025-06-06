namespace HotChocolate.Execution;

/// <summary>
/// Defines the possible event types of a request executor.
/// </summary>
public enum RequestExecutorEventType
{
    /// <summary>
    /// A request executor was created.
    /// </summary>
    Created,

    /// <summary>
    /// A request executor was evicted.
    /// </summary>
    Evicted
}
