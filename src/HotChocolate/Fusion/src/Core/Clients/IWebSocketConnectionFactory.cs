namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Provides a factory for creating new WebSocket connections.
/// </summary>
public interface IWebSocketConnectionFactory
{
    /// <summary>
    /// Creates a new WebSocket connection with the specified name.
    /// </summary>
    /// <param name="name">The name of the WebSocket connection.</param>
    /// <returns>An <see cref="IWebSocketConnection"/> object representing the new WebSocket connection.</returns>
    IWebSocketConnection CreateConnection(string name);
}
