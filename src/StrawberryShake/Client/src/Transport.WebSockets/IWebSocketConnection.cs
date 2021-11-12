using System.Text.Json;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// A WebSocket connection to a GraphQL server and allows to execute requests against it.
/// </summary>
public interface IWebSocketConnection : IConnection<JsonDocument>
{
}
