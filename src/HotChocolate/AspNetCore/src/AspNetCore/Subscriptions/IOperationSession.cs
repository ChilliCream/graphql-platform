using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Subscriptions;

/// <summary>
/// Represents a session with an execution engine subscription.
/// A subscription session is created within a <see cref="ISocketSession"/>.
/// Each socket session can have multiple subscription sessions open.
/// </summary>
public interface IOperationSession : IDisposable
{
    /// <summary>
    /// Gets the subscription id that the client has provided.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Specifies if this session is completed and will yield no further results.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Starts executing the operation.
    /// </summary>
    /// <param name="request">
    /// The graphql request.
    /// </param>
    /// <param name="completion">
    /// The completion handler that will be called when the operation is completed.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    void BeginExecute(
        GraphQLRequest request,
        IOperationSessionCompletionHandler completion,
        CancellationToken cancellationToken);
}

public interface IOperationSessionCompletionHandler
{
    void Complete(IOperationSession session);
}
