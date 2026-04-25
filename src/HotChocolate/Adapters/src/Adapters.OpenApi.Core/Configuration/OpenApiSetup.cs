namespace HotChocolate.Adapters.OpenApi.Configuration;

public sealed class OpenApiSetup
{
    public Func<IServiceProvider, IOpenApiDefinitionStorage>? StorageFactory { get; set; }

    internal Func<IServiceProvider, string, IDynamicEndpointDataSource>? EndpointDataSourceFactory { get; set; }

    internal Func<IServiceProvider, string, IDynamicOpenApiDocumentTransformer>? DocumentTransformerFactory { get; set; }
}
