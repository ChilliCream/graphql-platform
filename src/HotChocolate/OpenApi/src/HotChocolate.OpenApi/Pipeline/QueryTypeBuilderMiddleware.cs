using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Models;
using HotChocolate.Skimmed;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.OpenApi.Models;
using SharpYaml.Tokens;
using InputField = HotChocolate.Skimmed.InputField;
using ListType = HotChocolate.Skimmed.ListType;
using ObjectType = HotChocolate.Skimmed.ObjectType;

namespace HotChocolate.OpenApi.Pipeline;

internal sealed class QueryTypeBuilderMiddleware : IOpenApiWrapperMiddleware
{
    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        CreateQueryType(context);
        next.Invoke(context);
    }

    private static void CreateQueryType(OpenApiWrapperContext context)
    {
        var queryType = new ObjectType("Query");

        var queryOperations = context.GetQueryOperations();

        foreach (var operation in queryOperations)
        {
            var schema = operation.Value.Response?.Content.First().Value.Schema;
            if(schema is null) continue;

            var type = schema.GetGraphQLTypeNode(false);

            var outputField = new OutputField(operation.Value.OperationId.RemoveWhiteSpacesAndEnsureName())
            {
                Type = type
            };

            CreateType(context, type.NamedType().Name, schema.GetTypeSchema());

            queryType.Fields.Add(outputField);

            AddArguments(operation, outputField);
        }

        context.SkimmedSchema.QueryType = queryType;
    }

    private static void CreateType(OpenApiWrapperContext context, string typeName, OpenApiSchema schema)
    {
        if (context.SkimmedSchema.Types.ContainsName(typeName)  || Scalars.IsBuiltIn(typeName)) return;

        var type = new ObjectType(typeName)
        {
            Description = schema.Description
        };

        foreach (var property in schema.Properties)
        {
            var field = CreateField(context, schema.GetTypeSchema(), property);
            type.Fields.Add(field);
        }


        foreach (var allOf in schema.AllOf)
        {
            foreach (var allOfProperty in allOf.Properties)
            {
                var field = CreateField(context, schema.GetTypeSchema(), allOfProperty);
                type.Fields.Add(field);
            }
        }

        if (!context.SkimmedSchema.Types.ContainsName(typeName))
        {
            context.SkimmedSchema.Types.Add(type);
        }
    }

    private static OutputField CreateField(OpenApiWrapperContext context, OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property)
    {
        var isRequired = schema.Required.Contains(property.Key);
        var fieldType = property.Value.GetGraphQLTypeNode(isRequired);
        var field = new OutputField(property.Key)
        {
            Type = fieldType,
            Description = property.Value.Description
        };

        CreateType(context, fieldType.NamedType().Name, property.Value);
        return field;
    }

    private static void AddArguments(KeyValuePair<string, Operation> operation, OutputField outputField)
    {
        if (operation.Value.Arguments == null) return;

        foreach (var argument in operation.Value.Arguments)
        {
            if (argument.Parameter is { } parameter)
            {
                outputField.Arguments.Add(new InputField(parameter.Name, parameter.Schema.GetGraphQLTypeNode(false)));
            }

            if (argument.RequestBody is { } requestBody)
            {
                var requestSchema = requestBody.Content.FirstOrDefault().Value.Schema;
                if (requestSchema is null) continue;

                outputField.Arguments.Add(new InputField("value", requestSchema.GetGraphQLTypeNode(false)));
            }
        }
    }
}
