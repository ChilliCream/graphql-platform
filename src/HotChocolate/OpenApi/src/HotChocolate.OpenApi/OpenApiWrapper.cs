using HotChocolate.OpenApi.Pipeline;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal class OpenApiWrapper
{
    private readonly OpenApiWrapperDelegate _pipeline = 
        OpenApiWrapperPipelineBuilder.New()
            .Use<DiscoverOperationsMiddleware>()
            .Use<CreateInputTypesMiddleware>()
            .Use<CreatePayloadTypesMiddleware>()
            .Use<CreateQueryTypeMiddleware>()
            .Use<CreateMutationTypeMiddleware>()
            .Build();

    public Skimmed.Schema Wrap(string clientName, OpenApiDocument openApi)
    {
        var context = new OpenApiWrapperContext(clientName, openApi);
        _pipeline.Invoke(context);
        return context.MutableSchema;
    }
}
