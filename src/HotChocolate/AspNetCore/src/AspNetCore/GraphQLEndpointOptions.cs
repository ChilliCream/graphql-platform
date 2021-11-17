namespace HotChocolate.AspNetCore;

internal sealed class GraphQLEndpointOptions
{
    /// <summary>
    /// Gets or sets the GraphQL endpoint.
    /// If <see cref="GraphQLToolOptions.UseBrowserUrlAsGraphQLEndpoint"/> is
    /// set to <c>true</c> the GraphQL endpoint must be a relative path;
    /// otherwise, it must be an absolute URL.
    /// </summary>
    public string? GraphQLEndpoint { get; set; }
}
