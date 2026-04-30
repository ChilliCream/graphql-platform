namespace HotChocolate.Adapters.OpenApi.Configuration;

/// <summary>
/// Holds the per-schema configuration used to wire OpenAPI integration into the GraphQL server.
/// </summary>
public sealed class OpenApiSetup
{
    /// <summary>
    /// Gets or sets the factory that produces the <see cref="IOpenApiDefinitionStorage"/>
    /// instance backing this schema. The storage is the source of truth for OpenAPI
    /// definitions exposed through the gateway.
    /// </summary>
    public Func<IServiceProvider, IOpenApiDefinitionStorage>? StorageFactory { get; set; }
}
