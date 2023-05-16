namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represents the configuration of a GraphQL over HTTP client.
/// </summary>
internal sealed class HttpClientConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpClientConfiguration"/>.
    /// </summary>
    /// <param name="clientName">
    /// The name of the client.
    /// </param>
    /// <param name="subgraphName">
    /// The name of the subgraph that the client is connecting to.
    /// </param>
    /// <param name="endpointUri">
    /// The base address of the GraphQL over HTTP endpoint.
    /// </param>
    public HttpClientConfiguration(string clientName, string subgraphName, Uri endpointUri)
    {
        ClientName = clientName;
        SubgraphName = subgraphName;
        EndpointUri = endpointUri;
    }

    /// <summary>
    /// Gets the name of the client.
    /// </summary>
    public string ClientName { get; }

    /// <summary>
    /// Gets the name of the subgraph that the client is connecting to.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the URI of the GraphQL over HTTP endpoint.
    /// </summary>
    public Uri EndpointUri { get; }
}
