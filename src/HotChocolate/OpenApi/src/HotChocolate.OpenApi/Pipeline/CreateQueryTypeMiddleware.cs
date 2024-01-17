using System.Text.Json;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Skimmed;
using InputField = HotChocolate.Skimmed.InputField;
using ObjectType = HotChocolate.Skimmed.ObjectType;

namespace HotChocolate.OpenApi.Pipeline;

/// <summary>
/// Creates a query type based on all operations
/// </summary>
internal sealed class CreateQueryTypeMiddleware : IOpenApiWrapperMiddleware
{
    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        CreateQueryType(context);
        next.Invoke(context);
    }

    private static void CreateQueryType(OpenApiWrapperContext context)
    {
        var queryType = new ObjectType(OpenApiResources.RootTypeQuery);

        var queryOperations = context.GetQueryOperations();

        foreach (var operation in queryOperations)
        {
            var schema = operation.Value.Response?.Content.First().Value.Schema;

            if (schema is null)
            {
                continue;
            }

            var typeInfo = context.GetSchemaTypeInfo(schema);
            var type = typeInfo.GetGraphQLTypeNode(false);

            var outputField = new OutputField(OpenApiNamingHelper.GetFieldName(operation.Value.OperationId))
            {
                Type = type,
            };

            ObjectTypeFactory.ParseType(context, type.NamedType().Name, typeInfo.RootSchema);

            queryType.Fields.Add(outputField);

            AddArguments(context, operation, outputField);

            outputField.ContextData[OpenApiResources.ContextResolverParameter] = 
                OperationResolverHelper.CreateResolverFunc(context.ClientName, operation.Value);
        }

        context.MutableSchema.QueryType = queryType;
    }



    private static void AddArguments(
        OpenApiWrapperContext context, 
        KeyValuePair<string, Operation> operation, 
        OutputField outputField)
    {
        foreach (var parameter in operation.Value.Parameters)
        {
            var typeInfo = context.GetSchemaTypeInfo(parameter.Schema);
            outputField.Arguments.Add(new InputField(parameter.Name, typeInfo.GetGraphQLTypeNode(false)));
        }

        if (operation.Value.RequestBody is { } requestBody &&
            requestBody.Content.FirstOrDefault().Value.Schema is {} schema)
        {
            var typeInfo = context.GetSchemaTypeInfo(schema);
            outputField.Arguments.Add(new InputField("value", typeInfo.GetGraphQLTypeNode(false)));
        }
    }
}
