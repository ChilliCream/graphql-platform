using HotChocolate.OpenApi.Helpers;
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

        var mutationType = new ObjectType("Mutation");

        foreach (var operation in operations)
        {
            var outputField = new OutputField(operation.Value.OperationId.RemoveWhiteSpacesAndEnsureName())
            {
                Type = context.OperationPayloadTypeLookup[operation.Value.OperationId]
            };

            if (operation.Value.Arguments.Any())
            {
                var inputField = new InputField("input", context.OperationInputTypeLookup[operation.Value.OperationId]);
                outputField.Arguments.Add(inputField);
            }
            mutationType.Fields.Add(outputField);

            outputField.ContextData["resolver"] = new Func<IResolverContext, Task<string>>(async ctx =>
            {
                var resolver = OperationResolverHelper.CreateResolverFunc(operation.Value);
                return await resolver.Invoke(ctx);
            });
        }

        context.SkimmedSchema.MutationType = mutationType;
    }
}
