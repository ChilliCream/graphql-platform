namespace StrawberryShake.Transport.WebSockets.Protocols;

/// <summary>
/// Factory for <see cref="GraphQLWebSocketProtocol"/>
/// </summary>
public class GraphQLWebSocketProtocolFactory : ISocketProtocolFactory
{
    /// <inheritdoc />
    public string ProtocolName => "graphql-ws";

    /// <inheritdoc />
    public ISocketProtocol Create(ISocketClient socketClient)
    {
        return new GraphQLWebSocketProtocol(socketClient);
    }
}
