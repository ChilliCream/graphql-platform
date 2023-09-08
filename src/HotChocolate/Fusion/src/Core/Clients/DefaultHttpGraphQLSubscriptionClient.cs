using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Clients;

internal sealed class DefaultHttpGraphQLSubscriptionClient : IGraphQLSubscriptionClient
{
    private readonly HttpClientConfiguration _config;
    private readonly DefaultGraphQLHttpClient _client;

    public DefaultHttpGraphQLSubscriptionClient(
        HttpClientConfiguration configuration,
        HttpClient httpClient)
    {
        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }

        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _client = new DefaultGraphQLHttpClient(httpClient);
    }

    public string SubgraphName => _config.SubgraphName;

    public IAsyncEnumerable<GraphQLResponse> SubscribeAsync(
        SubgraphGraphQLRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return SubscribeInternalAsync(request, cancellationToken);
    }

    private async IAsyncEnumerable<GraphQLResponse> SubscribeInternalAsync(
        SubgraphGraphQLRequest subgraphRequest,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new GraphQLHttpRequest(subgraphRequest, _config.EndpointUri);
        using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);

        await foreach (var result in response.ReadAsResultStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return new GraphQLResponse(result);
        }
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return default;
    }
}
