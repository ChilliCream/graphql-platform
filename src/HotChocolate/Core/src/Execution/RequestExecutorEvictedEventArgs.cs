namespace HotChocolate.Execution;

/// <summary>
/// Represents the event arguments of a request executor evicted event.
/// </summary>
public sealed class RequestExecutorEvictedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="RequestExecutorEvictedEventArgs" />.
    /// </summary>
    /// <param name="name">
    /// The name of the request executor that is being evicted.
    /// </param>
    /// <param name="evictedExecutor">
    /// The request executor that is being evicted.
    /// </param>
    public RequestExecutorEvictedEventArgs(string name, IRequestExecutor evictedExecutor)
    {
        Name = name;
        EvictedExecutor = evictedExecutor;
    }

    /// <summary>
    /// Gets the name of the request executor that is being evicted.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the request executor that is being evicted.
    /// </summary>
    public IRequestExecutor EvictedExecutor { get; }
}
