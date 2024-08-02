using HotChocolate.Fusion.Metadata;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Clients;

internal sealed class DefaultWebSocketGraphQLSubscriptionClientFactory
    : IGraphQLSubscriptionClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebSocketConnectionFactory _connectionFactory;

    public DefaultWebSocketGraphQLSubscriptionClientFactory(
        IHttpClientFactory httpClientFactory,
        IWebSocketConnectionFactory connectionFactory)
    {
        _httpClientFactory = httpClientFactory ??
            throw new ArgumentNullException(nameof(httpClientFactory));
        _connectionFactory = connectionFactory ??
            throw new ArgumentNullException(nameof(connectionFactory));
    }

    public IGraphQLSubscriptionClient CreateClient(IGraphQLClientConfiguration configuration)
    {
        if (configuration is WebSocketClientConfiguration webSocketClientConfig)
        {
            var connection = _connectionFactory.CreateConnection(configuration.ClientName);
            return new DefaultWebSocketGraphQLSubscriptionClient(webSocketClientConfig, connection);
        }

        if (configuration is HttpClientConfiguration httpClientConfig)
        {
            var httpClient = _httpClientFactory.CreateClient(httpClientConfig.ClientName);
            return new DefaultHttpGraphQLSubscriptionClient(httpClientConfig, httpClient);
        }

        throw new ArgumentException(TransportConfigurationNotSupported, nameof(configuration));
    }
}
