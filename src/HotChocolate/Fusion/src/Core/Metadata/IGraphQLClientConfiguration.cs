namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represents the configuration of a GraphQL client.
/// </summary>
public interface IGraphQLClientConfiguration
{
    /// <summary>
    /// Gets the name of the client.
    /// </summary>
    public string ClientName { get; }

    /// <summary>
    /// Gets the name of the subgraph that the client is connecting to.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the URI of the GraphQL over WS endpoint.
    /// </summary>
    public Uri EndpointUri { get; }
}
