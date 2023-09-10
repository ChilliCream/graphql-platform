namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents the GraphQL HTTP options.
/// </summary>
public sealed class GraphQLHttpOptions
{
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
}
