namespace HotChocolate.Execution;

/// <summary>
/// The request executor resolver manages the configured request executors.
/// </summary>
public interface IRequestExecutorResolver
{
    /// <summary>
    /// An event that is raised when a request executor is being evicted.
    /// The consumers of a request executor shall subscribe to this event
    /// in order to release once this event is triggered.
    /// </summary>
    [Obsolete("Use the events property instead.")]
    event EventHandler<RequestExecutorEvictedEventArgs>? RequestExecutorEvicted;

    /// <summary>
    /// An event that is raised when a request executor is created or evicted.
    /// </summary>
    IObservable<RequestExecutorEvent> Events { get; }

    /// <summary>
    /// Gets or creates the request executor that is associated with the
    /// given configuration <paramref name="schemaName" />.
    /// </summary>
    /// <param name="schemaName">
    /// The schema name.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a request executor that is associated with the
    /// given configuration <paramref name="schemaName" />.
    /// </returns>
    ValueTask<IRequestExecutor> GetRequestExecutorAsync(
        string? schemaName = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers the eviction and disposal of the execution configuration
    /// with the specified name.
    /// It will not immediately remove it but inform the users of the associated
    /// <see cref="IRequestExecutor" /> that it is being evicted and that they
    /// should stop using it for new request.
    /// </summary>
    /// <param name="schemaName">
    /// The schema name.
    /// </param>
    void EvictRequestExecutor(string? schemaName = default);
}
