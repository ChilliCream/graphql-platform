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
    private readonly FederationQueryRewriter _queryRewriter;

    /// <summary>
    /// Initializes a new instance of <see cref="ApolloFederationSourceSchemaClientFactory"/>.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="queryRewriter">
    /// The query rewriter shared across all clients for this source schema.
    /// </param>
    internal ApolloFederationSourceSchemaClientFactory(
        IHttpClientFactory httpClientFactory,
        FederationQueryRewriter queryRewriter)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(queryRewriter);

        _httpClientFactory = httpClientFactory;
        _queryRewriter = queryRewriter;
    }

    /// <inheritdoc />
    protected override ISourceSchemaClient CreateClient(
        ApolloFederationSourceSchemaClientConfiguration configuration)
    {
        var httpClient = _httpClientFactory.CreateClient(configuration.HttpClientName);
        var graphQLClient = GraphQLHttpClient.Create(httpClient, disposeHttpClient: true);
        return new ApolloFederationSourceSchemaClient(graphQLClient, _queryRewriter);
    }
}
