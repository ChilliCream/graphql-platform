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

    /// <summary>
    /// Gets or sets the factory that produces the dynamic endpoint data source for a given
    /// schema. The data source materializes routes at runtime so OpenAPI definitions added
    /// after startup are reflected in the gateway's endpoint table.
    /// </summary>
    internal Func<IServiceProvider, string, IDynamicEndpointDataSource>? EndpointDataSourceFactory { get; set; }

    /// <summary>
    /// Gets or sets the factory that produces the OpenAPI document transformer for a given
    /// schema. The transformer is applied when the gateway emits its OpenAPI document so it
    /// stays in sync with the registered definitions.
    /// </summary>
    internal Func<IServiceProvider, string, IDynamicOpenApiDocumentTransformer>? DocumentTransformerFactory { get; set; }
}
