namespace HotChocolate.Adapters.OpenApi.Configuration;

internal sealed class OpenApiTransportSetup
{
    public Func<IDynamicEndpointDataSource>? EndpointDataSourceFactory { get; set; }

    public Func<IDynamicOpenApiDocumentTransformer>? DocumentTransformerFactory { get; set; }
}
