namespace HotChocolate.Execution;

/// <summary>
/// Represents the event arguments of a request executor evicted event.
/// </summary>
public sealed class RequestExecutorEvent : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="RequestExecutorEvent" />.
    /// </summary>
    /// <param name="type">
    /// The type of the event.
    /// </param>
    /// <param name="name">
    /// The name of the request executor that is being evicted.
    /// </param>
    /// <param name="executor">
    /// The request executor that is being evicted.
    /// </param>
    internal RequestExecutorEvent(
        RequestExecutorEventType type,
        string name,
        IRequestExecutor executor)
    {
        Type = type;
        Name = name;
        Executor = executor;
    }

    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    public RequestExecutorEventType Type { get; }

    /// <summary>
    /// Gets the name of the request executor that is being evicted.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the request executor that is being evicted.
    /// </summary>
    public IRequestExecutor Executor { get; }
}
