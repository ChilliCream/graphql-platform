using HotChocolate.Language;
using HotChocolate.OpenApi.Models;
using HotChocolate.Types;

namespace HotChocolate.OpenApi.Pipeline;

internal sealed class SchemaTypeBuilderMiddleware : IOpenApiWrapperMiddleware
{
    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        var responseSchemaReferences = context.Operations
            .Select(op => GetSchemaReference(op.Value))
            .OfType<string>()
            .Distinct()
            .ToList();

        foreach (var responseSchemaReference in responseSchemaReferences)
        {
            AddResponseObjectTypeDxescriptor(context, responseSchemaReference);
        }

        AddQueryType(context);
    }

    private static void AddQueryType(OpenApiWrapperContext context)
    {
        context.Query = descriptor =>
        {
            descriptor.Name("Query");

            var queryOperations = context.Operations
                .Where(o => o.Value.Method == HttpMethod.Get);

            foreach (var queryOperation in queryOperations)
            {
                var openApiSchema = queryOperation.Value.Response?.Content.First().Value.Schema;

                if (openApiSchema is null) continue;

                var isListType = openApiSchema.Items is not null;

                var typeName = isListType ? openApiSchema.Items!.Reference.Id : openApiSchema.Reference.Id;
                var graphQLName = GetGraphQLTypeName(typeName, false);
                var fieldDescriptor = descriptor
                    .Field(queryOperation.Key)
                    .Description(queryOperation.Value.Description)
                    .Type(isListType
                        ? new ListTypeNode(new NamedTypeNode(graphQLName))
                        : new NamedTypeNode(graphQLName))
                    .Resolve(_ => new object());

                foreach (var parameterEntry in queryOperation.Value.Parameter)
                {
                    fieldDescriptor.Argument(parameterEntry.Name,
                        argumentDescriptor => argumentDescriptor.Type(GetGraphQLTypeName(parameterEntry.Schema.Type, parameterEntry.Required)));
                }
            }
        };
    }

    private static void AddResponseObjectTypeDxescriptor(OpenApiWrapperContext context, string schemaReference)
    {
        var openApiSchema = context.GetSchema(schemaReference);

        if (openApiSchema is null)
        {
            return;
        }

        context.AddGraphQLType(descriptor =>
        {
            descriptor.Name(schemaReference);
            descriptor.Description(openApiSchema.Description);
            foreach (var keyValuePair in openApiSchema.Properties)
            {
                var isRequired = openApiSchema.Required.Contains(keyValuePair.Key);
                descriptor
                    .Field(keyValuePair.Key)
                    .Description(keyValuePair.Value.Description)
                    .Type(GetGraphQLTypeName(keyValuePair.Value.Type, isRequired))
                    .Resolve(_ => new object()); // todo add logic
            }
        });
    }

    private static string? GetSchemaReference(Operation operation)
    {
        var response = operation.Response;

        var content = response?.Content.FirstOrDefault();

        if (content?.Value is null) return null;

        if (content.Value.Value.Schema is null) return null;

        return content.Value.Value.Schema.Items is null
            ? content.Value.Value.Schema.Reference.Id
            : content.Value.Value.Schema.Items.Reference.Id;
    }

    private static string GetGraphQLTypeName(string openApiSchemaTypeName, bool required)
    {
        var typename = openApiSchemaTypeName switch
        {
            "string" => "String",
            "integer" => "Int",
            "boolean" => "Boolean",
            _ => openApiSchemaTypeName
        };

        var suffix = required ? "!" : string.Empty;
        return $"{typename}{suffix}";
    }

}
