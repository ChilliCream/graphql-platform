using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Clients;

internal sealed class DefaultHttpGraphQLClientFactory : IGraphQLClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DefaultHttpGraphQLClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ??
            throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public IGraphQLClient CreateClient(HttpClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var httpClient = _httpClientFactory.CreateClient(configuration.ClientName);
        return new DefaultHttpGraphQLClient(configuration, httpClient);
    }
}
