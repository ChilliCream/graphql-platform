using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Properties;
using HotChocolate.Skimmed;
using static HotChocolate.OpenApi.Helpers.OpenApiNamingHelper;

namespace HotChocolate.OpenApi.Pipeline;

/// <summary>
/// Creates the mutation type based on all operations
/// </summary>
internal sealed class CreateMutationTypeMiddleware : IOpenApiWrapperMiddleware
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

        if (operations.Count == 0)
        {
            return;
        }

        var mutationType = new ObjectType(OpenApiResources.RootTypeMutation);

        foreach (var operation in operations)
        {
            var description = operation.Value.Description;

            if (operation.Value.Response?.Description is { } responseDescription)
            {
                description += $"\n\nReturns: {responseDescription}";
            }

            var outputField = new OutputField(GetFieldName(operation.Value.OperationId))
            {
                Description = description,
                Type = context.OperationPayloadTypeLookup[operation.Value.OperationId],
            };

            if (operation.Value.Parameters.Count > 0 || operation.Value.RequestBody is not null)
            {
                var inputField = new InputField(
                    OpenApiResources.InputField,
                    context.OperationInputTypeLookup[operation.Value.OperationId])
                {
                    Description = operation.Value.RequestBody?.Description,
                };
                outputField.Arguments.Add(inputField);
            }
            mutationType.Fields.Add(outputField);

            outputField.ContextData[OpenApiResources.ContextResolverParameter] =
                OperationResolverHelper.CreateResolverFunc(context.ClientName, operation.Value);
        }

        context.MutableSchema.MutationType = mutationType;
    }
}
