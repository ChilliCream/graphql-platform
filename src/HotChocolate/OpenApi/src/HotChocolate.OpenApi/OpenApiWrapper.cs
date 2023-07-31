using HotChocolate.OpenApi.Pipeline;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal class OpenApiWrapper
{
    private readonly OpenApiWrapperDelegate _pipeline;

    public OpenApiWrapper()
    {
        _pipeline = OpenApiWrapperPipelineBuilder.New()
            .Use<OperationDiscoveryMiddleware>()
            .Use<SchemaTypeBuilderMiddleware>()
            .Build();
    }

    public OpenApiWrapperContext Wrap(OpenApiDocument openApi, OpenApiSpecVersion specVersion)
    {
        var context = new OpenApiWrapperContext(openApi, specVersion);
        _pipeline.Invoke(context);
        return context;
    }
}
