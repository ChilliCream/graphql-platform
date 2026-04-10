using HotChocolate.Fusion.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class HttpSourceSchemaClientFactory
    : SourceSchemaClientFactory<SourceSchemaHttpClientConfiguration>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpSourceSchemaClientFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClientFactory = httpClientFactory;
    }

    protected override ISourceSchemaClient CreateClient(
        SourceSchemaHttpClientConfiguration configuration)
    {
        var httpClient = _httpClientFactory.CreateClient(configuration.HttpClientName);
        httpClient.BaseAddress = configuration.BaseAddress;

        return new SourceSchemaHttpClient(
            GraphQLHttpClient.Create(httpClient, disposeHttpClient: true),
            configuration);
    }
}
