using HotChocolate.Fusion.Metadata;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Clients;

internal sealed class DefaultWebSocketGraphQLSubscriptionClientFactory
    : IGraphQLSubscriptionClientFactory
{
    private readonly IWebSocketConnectionFactory _connectionFactory;

    public DefaultWebSocketGraphQLSubscriptionClientFactory(
        IWebSocketConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ??
            throw new ArgumentNullException(nameof(connectionFactory));
    }

    public IGraphQLSubscriptionClient CreateClient(IGraphQLClientConfiguration configuration)
    {
        if (configuration is not WebSocketClientConfiguration webSocketClientConfig)
        {
            throw new ArgumentException(TransportConfigurationNotSupported, nameof(configuration));
        }

        var connection = _connectionFactory.CreateConnection(configuration.ClientName);
        return new DefaultWebSocketGraphQLSubscriptionClient(webSocketClientConfig, connection);
    }
}
