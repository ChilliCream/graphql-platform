using System.Buffers;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// Represents a GraphQL websocket protocol handler.
/// </summary>
public interface IProtocolHandler
{
    /// <summary>
    /// Gets the protocol name.
    /// </summary>
    string Name { get; }

    ValueTask OnReceiveAsync(
        ISocketSession session,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken = default);

    ValueTask OnConnectionInitTimeoutAsync(
        ISocketSession session,
        CancellationToken cancellationToken = default);

    ValueTask SendKeepAliveMessageAsync(
        ISocketSession session,
        CancellationToken cancellationToken = default);

    ValueTask SendResultMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IQueryResult result,
        CancellationToken cancellationToken = default);

    ValueTask SendErrorMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IReadOnlyList<IError> errors,
        CancellationToken cancellationToken = default);

    ValueTask SendCompleteMessageAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken = default);
}
