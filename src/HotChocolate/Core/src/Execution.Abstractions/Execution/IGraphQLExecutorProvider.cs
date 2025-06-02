namespace HotChocolate.Execution;

// call IRequestExecutorProvider
// break it up
public interface IGraphQLExecutorProvider
{
    public ValueTask<IGraphQLExecutor> GetExecutorAsync(
        string schemaName,
        CancellationToken cancellationToken = default);
}



// public IObservable<GraphQLExecutorEvent> Events { get; }

public sealed record GraphQLExecutorEvent(
    GraphQLExecutorEventType Type,
    string Name,
    IGraphQLExecutor Executor);

/// <summary>
/// Defines the possible event types of a request executor.
/// </summary>
public enum GraphQLExecutorEventType
{
    /// <summary>
    /// A request executor was created.
    /// </summary>
    Created,

    /// <summary>
    /// A request executor was evicted.
    /// </summary>
    Evicted,
}
