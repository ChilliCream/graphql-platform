using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represents the configuration of a GraphQL over HTTP client.
/// </summary>
public sealed record HttpClientConfiguration : IGraphQLClientConfiguration
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
    /// <param name="syntaxNode">
    /// The configuration syntax node.
    /// </param>
    public HttpClientConfiguration(
        string clientName,
        string subgraphName,
        Uri endpointUri,
        DirectiveNode? syntaxNode = null)
    {
        ClientName = clientName;
        SubgraphName = subgraphName;
        EndpointUri = endpointUri;
        SyntaxNode = syntaxNode;
    }

    /// <summary>
    /// Gets the name of the client.
    /// </summary>
    public string ClientName { get; init; }

    /// <summary>
    /// Gets the name of the subgraph that the client is connecting to.
    /// </summary>
    public string SubgraphName { get; init; }

    /// <summary>
    /// Gets the URI of the GraphQL over HTTP endpoint.
    /// </summary>
    public Uri EndpointUri { get; init; }

    /// <summary>
    /// Gets the configuration syntax node.
    /// </summary>
    public DirectiveNode? SyntaxNode { get; init; }
}
