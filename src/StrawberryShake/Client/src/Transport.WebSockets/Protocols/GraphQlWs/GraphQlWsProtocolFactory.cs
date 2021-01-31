namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Factory for <see cref="GraphQlWsProtocol"/>
    /// </summary>
    public class GraphQlWsProtocolFactory : ISocketProtocolFactory
    {
        /// <inheritdoc />
        public string ProtocolName => "graphql-ws";

        /// <inheritdoc />
        public ISocketProtocol Create(ISocketClient socketClient)
        {
            return new GraphQlWsProtocol(socketClient);
        }
    }
}
