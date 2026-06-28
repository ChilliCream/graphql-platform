using HotChocolate.AspNetCore;

namespace HotChocolate.Adapters.OpenApi;

internal sealed record OpenApiRegistration(
    OpenApiDefinitionRegistry Registry,
    HttpRequestExecutorProxy ExecutorProxy,
    IDynamicEndpointDataSource EndpointDataSource,
    IDynamicOpenApiDocumentTransformer DocumentTransformer);
