namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Provides a factory for creating new WebSocket connections.
/// </summary>
internal sealed class DefaultWebSocketConnectionFactory : IWebSocketConnectionFactory
{
    /// <summary>
    /// Creates a new WebSocket connection for a specified client configuration.
    /// </summary>
    /// <param name="name">
    /// The name of the client configuration.
    /// </param>
    /// <returns>
    /// An <see cref="IWebSocketConnection"/> object representing the new WebSocket connection.
    /// </returns>
    public IWebSocketConnection CreateConnection(string name)
        => new DefaultWebSocketConnection();
}
