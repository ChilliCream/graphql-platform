using HotChocolate.Execution.Configuration;
using HotChocolate.OpenApi.Pipeline;
using HotChocolate.Skimmed;
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
            .Use<InputTypeBuilderMiddleware>()
            .Use<PayloadTypeBuilderMiddleware>()
            .Use<QueryTypeBuilderMiddleware>()
            .Use<MutationTypeBuilderMiddleware>()
            .Build();
    }

    public Skimmed.Schema Wrap(OpenApiDocument openApi)
    {
        var context = new OpenApiWrapperContext(openApi);
        _pipeline.Invoke(context);
        return context.SkimmedSchema;
    }
}
