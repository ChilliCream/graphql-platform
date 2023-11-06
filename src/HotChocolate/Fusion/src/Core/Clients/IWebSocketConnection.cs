using System.Net.WebSockets;

namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Represents a WebSocket connection.
/// </summary>
public interface IWebSocketConnection : IDisposable
{
    /// <summary>
    /// Asynchronously connects to the specified WebSocket URI.
    /// </summary>
    /// <param name="uri">
    /// The URI of the WebSocket server to connect to.
    /// </param>
    /// <param name="subProtocol">
    /// The sub-protocol to use for the connection.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The <see cref="WebSocket"/> object that represents the connection is the result of the task.
    /// </returns>
    ValueTask<WebSocket> ConnectAsync(
        Uri uri,
        string subProtocol,
        CancellationToken cancellationToken = default);
}
