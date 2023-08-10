using System.Text.Json;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Skimmed;

namespace HotChocolate.OpenApi.Pipeline;

internal sealed class MutationTypeBuilderMiddleware : IOpenApiWrapperMiddleware
{
    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        CreateMutationType(context);
        next.Invoke(context);
    }

    private static void CreateMutationType(OpenApiWrapperContext context)
    {
        var operations = context.GetMutationOperations();

        if (operations.Count == 0) return;

        var mutationType = new ObjectType(OpenApiResources.RootTypeMutation);

        foreach (var operation in operations)
        {
            var outputField = new OutputField(OpenApiNamingHelper.GetFieldName(operation.Value.OperationId))
            {
                Type = context.OperationPayloadTypeLookup[operation.Value.OperationId]
            };

            if (operation.Value.Parameters.Count > 0 || operation.Value.RequestBody is not null)
            {
                var inputField = new InputField(OpenApiResources.InputField, context.OperationInputTypeLookup[operation.Value.OperationId]);
                outputField.Arguments.Add(inputField);
            }
            mutationType.Fields.Add(outputField);

            outputField.ContextData[OpenApiResources.ContextResolverParameter] = new Func<IResolverContext, Task<JsonElement>>(async ctx =>
            {
                var resolver = OperationResolverHelper.CreateResolverFunc(operation.Value);
                return await resolver.Invoke(ctx);
            });
        }

        context.SkimmedSchema.MutationType = mutationType;
    }
}
