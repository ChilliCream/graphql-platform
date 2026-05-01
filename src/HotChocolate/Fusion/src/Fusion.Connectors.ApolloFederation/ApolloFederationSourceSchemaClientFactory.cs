using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// A factory that creates <see cref="ApolloFederationSourceSchemaClient"/> instances
/// for source schemas configured with <see cref="ApolloFederationSourceSchemaClientConfiguration"/>.
/// </summary>
public sealed class ApolloFederationSourceSchemaClientFactory
    : SourceSchemaClientFactory<ApolloFederationSourceSchemaClientConfiguration>
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="ApolloFederationSourceSchemaClientFactory"/>.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public ApolloFederationSourceSchemaClientFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    protected override ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        ApolloFederationSourceSchemaClientConfiguration configuration)
    {
        var httpClient = _httpClientFactory.CreateClient(configuration.HttpClientName);
        httpClient.BaseAddress = configuration.BaseAddress;

        var graphQLClient = GraphQLHttpClient.Create(httpClient, disposeHttpClient: true);
        return new ApolloFederationSourceSchemaClient(graphQLClient, configuration.QueryRewriter);
    }
}
