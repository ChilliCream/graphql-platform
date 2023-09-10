using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Clients;

internal sealed class DefaultWebSocketGraphQLSubscriptionClient : WebSocketGraphQLSubscriptionClient
{
    public DefaultWebSocketGraphQLSubscriptionClient(
        WebSocketClientConfiguration configuration,
        IWebSocketConnection connection)
        : base(configuration, connection)
    {
    }
}
