namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

public interface IGraphQLOverWebSocketProtocolHandler : IProtocolHandler
{
    ValueTask SendPingMessageAsync(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload = null,
        CancellationToken cancellationToken = default);
}
