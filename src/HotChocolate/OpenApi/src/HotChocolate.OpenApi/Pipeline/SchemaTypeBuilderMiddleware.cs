using System.Text;
using HotChocolate.Language;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Models;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

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
            AddResponseObjectTypeDescriptor(context, responseSchemaReference);
        }

        AddQueryType(context);

        AddMutationType(context);
    }

    private static void AddQueryType(OpenApiWrapperContext context)
    {
        // context.Query = descriptor =>
        // {
        //     descriptor.Name("Query");
        //
        //     var queryOperations = context.Operations
        //         .Where(o => o.Value.Method == HttpMethod.Get);
        //
        //     foreach (var operation in queryOperations)
        //     {
        //         var openApiSchema = operation.Value.Response?.Content.First().Value.Schema;
        //
        //         if (openApiSchema is null) continue;
        //
        //         var fieldDescriptor = descriptor
        //             .Field(GetFieldName(operation.Value.OperationId))
        //             .Description(operation.Value.Description)
        //             .Type(GetGraphQLTypeNode(openApiSchema, false))
        //             .Resolve(async (resolverContext, token)=> await ResolveByRequest(resolverContext, operation, token));
        //
        //         AddQueryInput(operation, fieldDescriptor);
        //     }
        // };
    }

    private static void AddQueryInput(KeyValuePair<string, Operation> operation, IObjectFieldDescriptor fieldDescriptor)
    {
        if (operation.Value.Arguments is null) return;

        foreach (var argument in operation.Value.Arguments)
        {
            if (argument.Parameter is { } parameterEntry)
            {
                fieldDescriptor.Argument(GetFieldName(parameterEntry.Name),
                    argumentDescriptor => SetupArgumentOfOpenApiSchema(argumentDescriptor, parameterEntry.Schema, parameterEntry.Required));
            }

            if (argument.RequestBody is { } requestBody)
            {
                var requestBodySchema = requestBody.Content.FirstOrDefault().Value.Schema;

                if (requestBodySchema is null) continue;

                fieldDescriptor.Argument(GetFieldName(requestBodySchema.Reference.Id),
                    argumentDescriptor => SetupArgumentOfOpenApiSchema(argumentDescriptor, requestBodySchema, requestBody.Required));
            }
        }
    }

    private static void SetupArgumentOfOpenApiSchema(IArgumentDescriptor descriptor, OpenApiSchema schema, bool required)
    {
        descriptor.Type(GetGraphQLTypeNode(schema, required));
    }

    private static void AddMutationType(OpenApiWrapperContext context)
    {
        var mutationOperations = context.Operations
            .Where(o => o.Value.Method != HttpMethod.Get)
            .ToArray();

        var noMutationFields = mutationOperations
            .All(m => m.Value.Response?.Content?.FirstOrDefault().Value?.Schema is null);

        if (noMutationFields)
        {
            return;
        }

        // context.MutationType = descriptor =>
        // {
        //     descriptor.Name("Mutation");
        //
        //     foreach (var operation in mutationOperations)
        //     {
        //         var openApiSchema = operation.Value.Response?.Content.FirstOrDefault().Value?.Schema;
        //
        //         if (openApiSchema is null) continue;
        //
        //         descriptor
        //             .Field(GetFieldName(operation.Value.OperationId))
        //             .Description(operation.Value.Description)
        //             .Type(GetGraphQLTypeNode(openApiSchema, false))
        //             .Resolve(
        //                 async (resolverContext, token) => await ResolveByRequest(resolverContext, operation, token));
        //     }
        // };
    }

    private static async Task<string> ResolveByRequest(IResolverContext resolverContext, KeyValuePair<string, Operation> queryOperation,
        CancellationToken token)
    {
        var factory = resolverContext.Services.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("OpenApi");
        var result = await client.SendAsync(OperationHttpRequestHelper.CreateRequest(queryOperation.Value), token);
        return await result.Content.ReadAsStringAsync(token);
    }


    private static void AddResponseObjectTypeDescriptor(OpenApiWrapperContext context, string schemaReference)
    {
        var openApiSchema = context.GetSchema(schemaReference);

        if (openApiSchema is null)
        {
            return;
        }
        //
        // context.AddGraphQLType(descriptor =>
        // {
        //     descriptor.Name(NameUtils.MakeValidGraphQLName(schemaReference) ?? throw new InvalidOperationException("Type name can not be null"));
        //     descriptor.Description(openApiSchema.Description);
        //     foreach (var keyValuePair in openApiSchema.Properties)
        //     {
        //         var isRequired = openApiSchema.Required.Contains(keyValuePair.Key);
        //         descriptor
        //             .Field(GetFieldName(keyValuePair.Key))
        //             .Description(keyValuePair.Value.Description)
        //             .Type(GetGraphQLTypeNode(keyValuePair.Value, isRequired))
        //             .FromJson();
        //     }
        //     foreach (var allOf in openApiSchema.AllOf)
        //     {
        //         foreach (var allOfProperty in allOf.Properties)
        //         {
        //             var isRequired = allOf.Required.Contains(allOfProperty.Key);
        //
        //             descriptor
        //                 .Field(GetFieldName(allOfProperty.Key))
        //                 .Description(allOfProperty.Value.Description)
        //                 .Type(GetGraphQLTypeNode(allOfProperty.Value, isRequired))
        //                 .FromJson();
        //         }
        //     }
        // });
    }

    private static (string Name, string? Format, bool IsListType) GetSchemaTypeInfo(OpenApiSchema schema)
    {
        var isList = schema.Items is not null;

        var name = isList ? schema.Items!.Type : schema.Type;
        var format = isList ? schema.Items!.Format : schema.Format;

        name ??= isList ? schema.Items!.Reference.Id : schema.Reference.Id;

        return (name, format, isList);
    }

    private static ITypeNode GetGraphQLTypeNode(OpenApiSchema schema, bool required)
    {
        var (name, format, isListType) = GetSchemaTypeInfo(schema);
        var graphqlName = GetGraphQLTypeName(name, format);
        ITypeNode baseType = required
            ? new NonNullTypeNode(new NamedTypeNode(graphqlName))
            : new NamedTypeNode(graphqlName);

        return isListType
            ? new ListTypeNode(baseType)
            : baseType;
    }

    private static string? GetSchemaReference(Operation operation)
    {
        var response = operation.Response;

        var content = response?.Content.FirstOrDefault();

        if (content?.Value?.Schema is null) return null;

        return content.Value.Value.Schema.Items is null
            ? content.Value.Value.Schema.Reference.Id
            : content.Value.Value.Schema.Items.Reference.Id;
    }

    private static string GetGraphQLTypeName(string openApiSchemaTypeName, string format)
    {
        var typename = openApiSchemaTypeName switch
        {
            "string" => ScalarNames.String,
            "integer" => format == "int64" ? ScalarNames.Long : ScalarNames.Int,
            "boolean" => ScalarNames.Boolean,
            _ => NameUtils.MakeValidGraphQLName(openApiSchemaTypeName)
        };
        return typename ?? throw new InvalidOperationException();
    }

    private static string GetFieldName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder();
        var capitalizeNext = false;

        // Go through all the characters
        foreach (var currentChar in input)
        {
            // Only process alphabetic characters and spaces
            if (!char.IsLetter(currentChar) && currentChar != ' ') continue;
            if (currentChar == ' ')
            {
                capitalizeNext = true; // We want to capitalize the next character
            }
            else if (capitalizeNext)
            {
                sb.Append(char.ToUpper(currentChar));
                capitalizeNext = false; // Reset flag after capitalizing
            }
            else
            {
                sb.Append(char.ToLower(currentChar));
            }
        }

        return NameUtils.MakeValidGraphQLName(sb.ToString()) ?? throw new InvalidOperationException("Field name can not be null");
    }
}
