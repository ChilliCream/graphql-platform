using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Creates <see cref="AliasBatchingHttpSourceSchemaClient"/> instances for HTTP source schemas
/// that opt in to alias batching. Registered before <see cref="HttpSourceSchemaClientFactory"/>
/// so that alias-batched configurations select this factory while all other HTTP configurations
/// fall through to the default client.
/// </summary>
internal sealed class AliasBatchingHttpSourceSchemaClientFactory : ISourceSchemaClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AliasBatchingHttpSourceSchemaClientFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public bool CanHandle(ISourceSchemaClientConfiguration configuration)
        => configuration is HttpSourceSchemaClientConfiguration { AliasBatching: true };

    /// <inheritdoc />
    public ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        ISourceSchemaClientConfiguration configuration)
    {
        if (configuration is not HttpSourceSchemaClientConfiguration casted)
        {
            throw ThrowHelper.InvalidClientConfiguration(
                typeof(HttpSourceSchemaClientConfiguration),
                configuration.GetType());
        }

        var httpClient = _httpClientFactory.CreateClient(casted.HttpClientName);
        httpClient.BaseAddress = casted.BaseAddress;

        return new AliasBatchingHttpSourceSchemaClient(
            GraphQLHttpClient.Create(httpClient, disposeHttpClient: true),
            casted);
    }
}
