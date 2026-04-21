namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Configuration for an Apollo Federation subgraph source schema client
/// that sends queries over HTTP using the <c>_entities</c> protocol.
/// </summary>
public sealed class ApolloFederationSourceSchemaClientConfiguration : ISourceSchemaClientConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="ApolloFederationSourceSchemaClientConfiguration"/>.
    /// </summary>
    /// <param name="name">The name of the source schema.</param>
    /// <param name="httpClientName">
    /// The name of the <see cref="HttpClient"/> to resolve from
    /// <see cref="IHttpClientFactory"/>.
    /// </param>
    /// <param name="supportedOperations">The supported operation types.</param>
    public ApolloFederationSourceSchemaClientConfiguration(
        string name,
        string httpClientName,
        SupportedOperationType supportedOperations = SupportedOperationType.Query | SupportedOperationType.Mutation)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(httpClientName);

        Name = name;
        HttpClientName = httpClientName;
        SupportedOperations = supportedOperations;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Gets the name of the underlying HTTP client.
    /// </summary>
    public string HttpClientName { get; }

    /// <inheritdoc />
    public SupportedOperationType SupportedOperations { get; }
}
