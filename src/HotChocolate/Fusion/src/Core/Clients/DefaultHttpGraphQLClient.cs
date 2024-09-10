using System.Text.Json;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Transport.Http;
using static HotChocolate.Fusion.Clients.TransportFeatures;

namespace HotChocolate.Fusion.Clients;

internal sealed class DefaultHttpGraphQLClient : IGraphQLClient
{
    private readonly HttpClientConfiguration _config;
    private readonly DefaultGraphQLHttpClient _client;

    public DefaultHttpGraphQLClient(
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

    public Task<GraphQLResponse> ExecuteAsync(SubgraphGraphQLRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return ExecuteInternalAsync(request, cancellationToken);
    }

    private async Task<GraphQLResponse> ExecuteInternalAsync(SubgraphGraphQLRequest subgraphRequest, CancellationToken ct)
    {
        try
        {
            var request = new GraphQLHttpRequest(subgraphRequest, _config.EndpointUri);

            if((subgraphRequest.TransportFeatures & FileUpload) == FileUpload)
            {
                request.EnableFileUploads = true;
            }

            using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);
            var result = await response.ReadAsResultAsync(ct).ConfigureAwait(false);
            return new GraphQLResponse(result);
        }
        catch (Exception exception)
        {
            return new GraphQLResponse(exception);
        }
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return default;
    }
}
