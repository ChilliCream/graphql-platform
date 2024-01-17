namespace HotChocolate.AspNetCore;

/// <summary>
/// Defines if middlewares are explicitly hosted through routing or of they are all
/// included into one route.
/// </summary>
public enum MiddlewareRoutingType
{
    /// <summary>
    /// Integrated into one route e.g. MapGraphQL()
    /// </summary>
    Integrated,

    /// <summary>
    /// Explicitly hosted e.g. MapGraphQLSchema()
    /// </summary>
    Explicit,
}
