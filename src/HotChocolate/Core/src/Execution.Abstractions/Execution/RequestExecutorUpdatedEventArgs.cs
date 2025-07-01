namespace HotChocolate.Execution;

/// <summary>
/// Represents the event arguments for the <see cref="RequestExecutorProxy.ExecutorUpdated"/> event.
/// </summary>
public sealed class RequestExecutorUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="RequestExecutorUpdatedEventArgs" />.
    /// </summary>
    /// <param name="executor">
    /// The request executor that was updated.
    /// </param>
    public RequestExecutorUpdatedEventArgs(IRequestExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);

        Executor = executor;
    }

    /// <summary>
    /// Gets the request executor that was updated.
    /// </summary>
    public IRequestExecutor Executor { get; }
}
