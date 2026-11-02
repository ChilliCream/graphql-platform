using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Subscriptions;

/// <summary>
/// The operation manager provides access to registered running operation within a socket session.
/// The operation manager ensures that operation are correctly tracked and cleaned up after they
/// have been completed.
/// </summary>
public interface IOperationManager
    : IEnumerable<IOperationSession>
    , IDisposable
{
    /// <summary>
    /// Enqueues a request for execution with the operation manager.
    /// </summary>
    /// <param name="sessionId">
    /// The operation sessionId given by the client. The sessionId must be unique within the
    /// <see cref="ISocketSession"/>.
    /// </param>
    /// <param name="request">
    /// The GraphQL request that shall be executed.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the <paramref name="request"/>
    /// was accepted and registered for execution.
    /// </returns>
    bool Enqueue(string sessionId, GraphQLRequest request);

    /// <summary>
    /// Enqueues a batch of requests for execution with the operation manager.
    /// </summary>
    /// <param name="sessionId">
    /// The operation sessionId given by the client. The sessionId must be unique within the
    /// <see cref="ISocketSession"/>.
    /// </param>
    /// <param name="requests">
    /// The GraphQL requests that shall be executed as a batch.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the <paramref name="requests"/>
    /// were accepted and registered for execution.
    /// </returns>
    bool EnqueueBatch(string sessionId, GraphQLRequest[] requests);

    /// <summary>
    /// Completes a request that was previously enqueued with the operation manager.
    /// </summary>
    /// <param name="sessionId">
    /// The operation sessionId given by the client. The sessionId must be unique within the
    /// <see cref="ISocketSession"/>.
    /// </param>
    /// <returns>
    /// Returns <c>true</c> if the operation was still executing and managed by this
    /// operation manager. Returns <c>false</c> if no operation existed with the provided
    /// <paramref name="sessionId"/> within this operation manager.
    /// </returns>
    bool Complete(string sessionId);
}
