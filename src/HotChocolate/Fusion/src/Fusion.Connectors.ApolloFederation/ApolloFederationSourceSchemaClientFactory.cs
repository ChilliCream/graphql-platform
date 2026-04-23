using System.Collections.Concurrent;
using HotChocolate.Fusion.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// A factory that creates <see cref="ApolloFederationSourceSchemaClient"/> instances
/// for source schemas configured with <see cref="ApolloFederationSourceSchemaClientConfiguration"/>.
/// </summary>
public sealed class ApolloFederationSourceSchemaClientFactory
    : SourceSchemaClientFactory<ApolloFederationSourceSchemaClientConfiguration>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, FederationQueryRewriter> _rewritersBySchema = new();

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
        ApolloFederationSourceSchemaClientConfiguration configuration)
    {
        var httpClient = _httpClientFactory.CreateClient(configuration.HttpClientName);
        httpClient.BaseAddress = configuration.BaseAddress;

        var queryRewriter = _rewritersBySchema.GetOrAdd(
            configuration.Name,
            static (_, config) => new FederationQueryRewriter(config.Lookups, config.EntityRequires),
            configuration);

        var graphQLClient = GraphQLHttpClient.Create(httpClient, disposeHttpClient: true);
        return new ApolloFederationSourceSchemaClient(graphQLClient, queryRewriter);
    }
}
