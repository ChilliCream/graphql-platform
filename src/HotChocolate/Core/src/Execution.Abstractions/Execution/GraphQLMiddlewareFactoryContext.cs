using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// This context is available when creating a middleware pipeline.
/// </summary>
public class GraphQLMiddlewareFactoryContext : IFeatureProvider
{
    /// <summary>
    /// Gets the GraphQL schema name.
    /// </summary>
    public required ISchemaDefinition Schema { get; init; }

    /// <summary>
    /// Gets the application level service provider.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public IFeatureCollection Features { get; } = new FeatureCollection();
}
