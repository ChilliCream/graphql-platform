using System.Text.Json;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Models;
using HotChocolate.OpenApi.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Skimmed;
using ObjectType = HotChocolate.Skimmed.ObjectType;

namespace HotChocolate.OpenApi.Pipeline;

internal sealed class PayloadTypeBuilderMiddleware : IOpenApiWrapperMiddleware
{
    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        var operations = context.GetMutationOperations();

        foreach (var operation in operations)
        {
            CreatePayloadType(context, operation.Value);
        }

        next.Invoke(context);
    }

    private static void CreatePayloadType(OpenApiWrapperContext context, Operation operation)
    {
        var typeName = OpenApiNamingHelper.GetPayloadTypeName(operation.OperationId);

        var schema = operation.Response?.Content.FirstOrDefault().Value?.Schema;
        if (schema is null)
        {
            var payloadType = new ObjectType(typeName);
            var field = new OutputField(OpenApiResources.PayloadSuccessField)
            {
                Type = new ObjectType(nameof(Boolean))
            };
            payloadType.Fields.Add(field);
            context.SkimmedSchema.Types.Add(payloadType);
            context.OperationPayloadTypeLookup[operation.OperationId] = payloadType;
        }
        else
        {
            var payloadType = ObjectTypeFactory.CreateType(context, typeName, schema);
            context.OperationPayloadTypeLookup[operation.OperationId] = payloadType;
        }
    }
}
