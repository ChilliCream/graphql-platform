namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents the GraphQL server options.
/// </summary>
public class GraphQLServerOptions
{
    /// <summary>
    /// Gets the GraphQL tool options for Banana Cake Pop.
    /// </summary>
    public GraphQLToolOptions Tool { get; internal set; } = new();

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
    /// Defines if GraphQL HTTP Multipart requests are allowed.
    /// </summary>
    public bool EnableMultipartRequests { get; set; } = true;

    /// <summary>
    /// Defines if the GraphQL schema SDL can be downloaded.
    /// </summary>
    public bool EnableSchemaRequests { get; set; } = true;
}
