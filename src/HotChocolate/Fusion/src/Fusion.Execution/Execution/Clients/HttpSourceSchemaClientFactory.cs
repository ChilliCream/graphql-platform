using HotChocolate.Fusion.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class HttpSourceSchemaClientFactory
    : SourceSchemaClientFactory<HttpSourceSchemaClientConfiguration>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpSourceSchemaClientFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClientFactory = httpClientFactory;
    }

    protected override ISourceSchemaClient CreateClient(
        HttpSourceSchemaClientConfiguration configuration)
    {
        var httpClient = _httpClientFactory.CreateClient(configuration.HttpClientName);
        httpClient.BaseAddress = configuration.BaseAddress;

        return new HttpSourceSchemaClient(
            GraphQLHttpClient.Create(httpClient, disposeHttpClient: true),
            configuration);
    }
}
