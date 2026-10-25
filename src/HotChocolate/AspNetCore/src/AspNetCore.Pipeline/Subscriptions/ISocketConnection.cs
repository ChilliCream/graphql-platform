using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Features;
using HotChocolate.Transport.Sockets;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions;

/// <summary>
/// The socket connection represents an accepted connection with a socket
/// where the protocol is already negotiated.
/// </summary>
public interface ISocketConnection : ISocket, IFeatureProvider, IDisposable
{
    /// <summary>
    /// Gets access to the HTTP Context.
    /// </summary>
    HttpContext HttpContext { get; }

    /// <summary>
    /// Gets access to the request scoped service provider.
    /// </summary>
    IServiceProvider RequestServices { get; }

    /// <summary>
    /// Get the request cancellation token.
    /// </summary>
    CancellationToken RequestAborted { get; }

    /// <summary>
    /// Specifies if the connection is connected to a client.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Specifies if a connection initialization message has been received from the client.
    /// This is set as soon as the message arrives, before the connection is accepted, so that
    /// the initialization timeout only reflects whether the client sent the message in time and
    /// not how long the acceptance (for example authentication) takes.
    /// </summary>
    bool ConnectionInitReceived { get; }

    /// <summary>
    /// Tries to accept the connection and returns the accepted protocol handler.
    /// </summary>
    Task<IProtocolHandler?> TryAcceptConnection();

    /// <summary>
    /// Send a message to the client.
    /// </summary>
    /// <param name="message">
    /// The message.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask SendAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the connection with the client.
    /// </summary>
    /// <param name="message">
    /// A human readable message explaining the close reason.
    /// </param>
    /// <param name="reason">
    /// The message close reason.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask CloseAsync(
        string message,
        ConnectionCloseReason reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the connection with the client.
    /// </summary>
    /// <param name="message">
    /// A human readable message explaining the close reason.
    /// </param>
    /// <param name="reason">
    /// The message close reason.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask CloseAsync(
        string message,
        int reason,
        CancellationToken cancellationToken = default);
}
