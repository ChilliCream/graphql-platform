namespace HotChocolate.AspNetCore;

/// <summary>
/// Defines the types of batching that are allowed on the GraphQL server.
/// </summary>
[Flags]
public enum AllowedBatching
{
    /// <summary>
    /// No batching is allowed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Allows variable batching, where a single operation is executed
    /// multiple times with different sets of variables.
    /// </summary>
    VariableBatching = 1,

    /// <summary>
    /// Allows request batching, where multiple independent GraphQL
    /// operations are sent as a JSON array in a single HTTP request.
    /// </summary>
    RequestBatching = 2,

    /// <summary>
    /// Allows all types of batching.
    /// </summary>
    All = VariableBatching | RequestBatching
}
