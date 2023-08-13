using HotChocolate.OpenApi.Pipeline;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal class OpenApiWrapper
{
    private readonly OpenApiWrapperDelegate _pipeline;

    public OpenApiWrapper()
    {
        _pipeline = OpenApiWrapperPipelineBuilder.New()
            .Use<DiscoverOperationsMiddleware>()
            .Use<CreateInputTypesMiddleware>()
            .Use<CreatePayloadTypesMiddleware>()
            .Use<CreateQueryTypeMiddleware>()
            .Use<CreateMutationTypeMiddleware>()
            .Build();
    }

    public Skimmed.Schema Wrap(OpenApiDocument openApi)
    {
        var context = new OpenApiWrapperContext(openApi);
        _pipeline.Invoke(context);
        return context.SkimmedSchema;
    }
}
