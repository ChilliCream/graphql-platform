using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Transport.Sockets;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions;

/// <summary>
/// The socket connection represent an accepted connection with a socket
/// where the protocol is already negotiated.
/// </summary>
public interface ISocketConnection : ISocket, IHasContextData, IDisposable
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
