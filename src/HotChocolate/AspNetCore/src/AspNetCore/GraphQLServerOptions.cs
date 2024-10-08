namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents the GraphQL server options.
/// </summary>
public sealed class GraphQLServerOptions
{
    /// <summary>
    /// Gets the GraphQL tool options for Nitro.
    /// </summary>
    public GraphQLToolOptions Tool { get; internal set; } = new();

    /// <summary>
    /// Gets the GraphQL socket options.
    /// </summary>
    public GraphQLSocketOptions Sockets { get; internal set; } = new();

    /// <summary>
    /// Gets or sets which GraphQL options are allowed on GET requests.
    /// </summary>
    public AllowedGetOperations AllowedGetOperations { get; set; } =
        AllowedGetOperations.Query;

    /// <summary>
    /// Defines if GraphQL HTTP GET requests are allowed.
    /// </summary>
    public bool EnableGetRequests { get; set; } = true;

    /// <summary>
    /// Defines if GraphQL HTTP GET requests are allowed.
    /// </summary>
    public bool EnforceGetRequestsPreflightHeader { get; set; } = false;

    /// <summary>
    /// Defines if GraphQL HTTP Multipart requests are allowed.
    /// </summary>
    public bool EnableMultipartRequests { get; set; } = true;

    /// <summary>
    /// Defines if preflight headers are enforced for multipart requests.
    /// </summary>
    public bool EnforceMultipartRequestsPreflightHeader { get; set; } = true;

    /// <summary>
    /// Defines if the GraphQL schema SDL can be downloaded.
    /// </summary>
    public bool EnableSchemaRequests { get; set; } = true;

    /// <summary>
    /// Defines if request batching is enabled.
    /// </summary>
    public bool EnableBatching { get; set; }
}
