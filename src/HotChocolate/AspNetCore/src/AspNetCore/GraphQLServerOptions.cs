using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    /// <summary>
    /// Represents the GraphQL server options.
    /// </summary>
    public class GraphQLServerOptions
    {
        /// <summary>
        /// Gets the GraphQL tool options for Banana Cake Pop.
        /// </summary>
        public GraphQLToolOptions Tool { get; } = new();

        /// <summary>
        /// Gets or sets which GraphQL options are allowed on GET requests.
        /// </summary>
        public AllowedGetOperations AllowedGetOperations { get; set; } =
            AllowedGetOperations.Query;
    }

    /// <summary>
    /// Represents the GraphQL tool options for Banana Cake Pop.
    /// </summary>
    public class GraphQLToolOptions
    {
        /// <summary>
        /// Gets or sets the default document content.
        /// </summary>
        public string? Document { get; set; }

        /// <summary>
        /// Gets or sets the default method.
        /// </summary>
        public DefaultCredentials? Credentials { get; set; }

        /// <summary>
        /// Gets or sets the default http headers for Banana Cake Pop.
        /// </summary>
        public IHeaderDictionary? HttpHeaders { get; set; }

        /// <summary>
        /// Gets or sets the default
        /// </summary>
        public DefaultHttpMethod? HttpMethod { get; set; }

        /// <summary>
        /// Defines if Banana Cake Pop is enabled.
        /// </summary>
        public bool Enable { get; set; } = true;
    }

    public enum DefaultCredentials
    {
        Include,
        Omit,
        SameOrigin,
    }

    public enum DefaultHttpMethod
    {
        Get,
        Post
    }

    public enum AllowedGetOperations
    {
        Query,
        QueryAndMutation
    }
}
