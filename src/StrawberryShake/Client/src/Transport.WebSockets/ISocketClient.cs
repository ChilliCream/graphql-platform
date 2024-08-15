using System.IO.Pipelines;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a client for sending and receiving messages responses over a abstract socket
/// identified by a URI and name.
/// </summary>
public interface ISocketClient : IAsyncDisposable
{
    /// <summary>
    /// An event that is called when the message receiving cycle stopped
    /// </summary>
    event EventHandler OnConnectionClosed;

    /// <summary>
    /// The URI where the socket should connect to
    /// </summary>
    Uri? Uri { get; set; }

    /// <summary>
    /// The <see cref="ISocketConnectionInterceptor"/> of this client
    /// </summary>
    ISocketConnectionInterceptor ConnectionInterceptor { get; set; }

    /// <summary>
    /// The name of the socket
    /// </summary>
    string Name { get; }

    /// <summary>
    /// If the socket is open or closed
    /// </summary>
    bool IsClosed { get; }

    /// <summary>
    /// Sends data asynchronously to a connected Socket object.
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    /// <returns>A task that is completed once the message is send</returns>
    ValueTask SendAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins an asynchronous request to receive data from a connected Socket object and writes
    /// it into the provider <see cref="PipeWriter"/>
    /// </summary>
    /// <param name="writer">The writer where the received data is written to</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    ValueTask ReceiveAsync(
        PipeWriter writer,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a connection to the server
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    /// <returns>The socket protocol that was established with the server</returns>
    Task<ISocketProtocol> OpenAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a connection to the server
    /// </summary>
    /// <param name="message">A message why the connection was closes</param>
    /// <param name="closeStatus">The close status on how the socket was closes</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation</param>
    /// <returns></returns>
    Task CloseAsync(
        string message,
        SocketCloseStatus closeStatus,
        CancellationToken cancellationToken = default);
}
