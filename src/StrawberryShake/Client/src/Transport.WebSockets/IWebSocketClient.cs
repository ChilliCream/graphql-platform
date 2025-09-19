using System.Net.WebSockets;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a client for sending and receiving messages responses over a websocket
/// identified by a URI and name.
/// </summary>
public interface IWebSocketClient : ISocketClient
{
    /// <summary>
    /// The <see cref="WebSocket"/> that is used to communicate with the server
    /// </summary>
    WebSocket Socket { get; }
}
