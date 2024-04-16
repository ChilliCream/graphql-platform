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

            var payloadType = context.OperationPayloadTypeLookup[operation.Value.OperationId];

            var outputField = new OutputField(GetFieldName(operation.Value.OperationId))
            {
                Description = description,
                Type = IsPayloadTypeNullable(operation.Value)
                    ? payloadType
                    : new NonNullType(payloadType),
            };

            if (operation.Value.Parameters.Count > 0 || operation.Value.RequestBody is not null)
            {
                var inputType = context.OperationInputTypeLookup[operation.Value.OperationId];

                var inputField = new InputField(OpenApiResources.InputField)
                {
                    Description = operation.Value.RequestBody?.Description,
                    Type = IsInputTypeNullable(operation.Value)
                        ? inputType
                        : new NonNullType(inputType),
                };
                outputField.Arguments.Add(inputField);
            }
            mutationType.Fields.Add(outputField);

            outputField.ContextData[OpenApiResources.ContextResolverParameter] =
                OperationResolverHelper.CreateResolverFunc(context.ClientName, operation.Value);
        }

        context.MutableSchema.MutationType = mutationType;
    }

    private static bool IsPayloadTypeNullable(Operation operation)
    {
        return operation.Response?.Content.Values.FirstOrDefault()?.Schema.Nullable == true;
    }

    private static bool IsInputTypeNullable(Operation operation)
    {
        return
            operation.Parameters.Count == 0 &&
            operation.RequestBody?.Content.Values.FirstOrDefault()?.Schema.Nullable == true;
    }
}
