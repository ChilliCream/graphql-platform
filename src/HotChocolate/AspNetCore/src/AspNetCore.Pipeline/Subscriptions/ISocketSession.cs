using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace HotChocolate.AspNetCore.Subscriptions;

/// <summary>
/// Represents a GraphQL server socket session.
/// </summary>
public interface ISocketSession : IDisposable
{
    /// <summary>
    /// Gets access to the socket connection.
    /// </summary>
    ISocketConnection Connection { get; }

    /// <summary>
    /// Gets access to the active protocol handler.
    /// </summary>
    IProtocolHandler Protocol { get; }

    /// <summary>
    /// Gets access to the subscription manager of this connection.
    /// </summary>
    IOperationManager Operations { get; }
}
