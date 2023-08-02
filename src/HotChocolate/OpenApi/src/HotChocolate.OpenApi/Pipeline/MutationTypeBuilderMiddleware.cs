using HotChocolate.OpenApi.Helpers;
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
        }

        context.SkimmedSchema.MutationType = mutationType;
    }
}
