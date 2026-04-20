using ChilliCream.Nitro.App;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents the GraphQL server options.
/// </summary>
public sealed class GraphQLServerOptions
{
    /// <summary>
    /// Gets the Nitro tool options.
    /// </summary>
    public NitroAppOptions Tool { get; internal set; } = new();

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
    /// Specifies that the transport is allowed to provide the schema SDL document as a file.
    /// </summary>
    public bool EnableSchemaFileSupport { get; set; } = true;

    /// <summary>
    /// Defines if GraphQL HTTP GET requests are allowed.
    /// </summary>
    public bool EnableGetRequests { get; set; } = true;

    /// <summary>
    /// Defines if GraphQL HTTP GET requests are allowed.
    /// </summary>
    public bool EnforceGetRequestsPreflightHeader { get; set; }

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
    /// Gets or sets which types of batching are allowed.
    /// </summary>
    public AllowedBatching Batching { get; set; } = AllowedBatching.None;

    /// <summary>
    /// Gets or sets the maximum number of operations allowed in a single batch.
    /// A value of 0 means unlimited. Defaults to 1024.
    /// </summary>
    public int MaxBatchSize { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the maximum number of concurrent GraphQL executions that can be
    /// processed simultaneously. A value of <c>null</c> means unlimited. Defaults to 64.
    /// </summary>
    public int? MaxConcurrentExecutions { get; set; } = 64;

    internal GraphQLServerOptions Clone()
        => new()
        {
            Tool = Tool.Clone(),
            Sockets = new GraphQLSocketOptions
            {
                ConnectionInitializationTimeout = Sockets.ConnectionInitializationTimeout,
                KeepAliveInterval = Sockets.KeepAliveInterval
            },
            AllowedGetOperations = AllowedGetOperations,
            EnableSchemaFileSupport = EnableSchemaFileSupport,
            EnableGetRequests = EnableGetRequests,
            EnforceGetRequestsPreflightHeader = EnforceGetRequestsPreflightHeader,
            EnableMultipartRequests = EnableMultipartRequests,
            EnforceMultipartRequestsPreflightHeader = EnforceMultipartRequestsPreflightHeader,
            EnableSchemaRequests = EnableSchemaRequests,
            Batching = Batching,
            MaxBatchSize = MaxBatchSize,
            MaxConcurrentExecutions = MaxConcurrentExecutions
        };
}
