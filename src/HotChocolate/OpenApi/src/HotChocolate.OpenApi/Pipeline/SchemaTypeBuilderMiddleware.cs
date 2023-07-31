using System.Text;
using HotChocolate.Language;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Models;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

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
                    .Field(GetFieldName(queryOperation.Value.OperationId))
                    .Description(queryOperation.Value.Description)
                    .Type(isListType
                        ? new ListTypeNode(new NamedTypeNode(graphQLName))
                        : new NamedTypeNode(graphQLName))
                    .Resolve(async (resolverContext, token)=> await ResolveByRequest(resolverContext, queryOperation, token));

                // foreach (var parameterEntry in queryOperation.Value.Parameter)
                // {
                //     fieldDescriptor.Argument(parameterEntry.Name,
                //         argumentDescriptor => argumentDescriptor.Type(GetGraphQLTypeName(parameterEntry.Schema.Type, parameterEntry.Required)));
                // }
            }
        };
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

        context.MutationType = descriptor =>
        {
            descriptor.Name("Mutation");

            foreach (var operation in mutationOperations)
            {
                var openApiSchema = operation.Value.Response?.Content.FirstOrDefault().Value?.Schema;

                if (openApiSchema is null) continue;

                descriptor
                    .Field(GetFieldName(operation.Value.OperationId))
                    .Description(operation.Value.Description)
                    .Type(GetGraphQLTypeName(openApiSchema.Reference.Id, false))
                    .Resolve(
                        async (resolverContext, token) => await ResolveByRequest(resolverContext, operation, token));
            }
        };
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

        context.AddGraphQLType(descriptor =>
        {
            descriptor.Name(schemaReference);
            descriptor.Description(openApiSchema.Description);
            foreach (var keyValuePair in openApiSchema.Properties)
            {
                var isRequired = openApiSchema.Required.Contains(keyValuePair.Key);
                descriptor
                    .Field(GetFieldName(keyValuePair.Key))
                    .Description(keyValuePair.Value.Description)
                    .Type(GetGraphQLTypeName(keyValuePair.Value.Type, isRequired))
                    .FromJson();
            }
            foreach (var allOf in openApiSchema.AllOf)
            {
                foreach (var allOfProperty in allOf.Properties)
                {

                    var isRequired = allOf.Required.Contains(allOfProperty.Key);
                    descriptor
                        .Field(GetFieldName(allOfProperty.Key))
                        .Description(allOfProperty.Value.Description)
                        .Type(GetGraphQLTypeName(allOfProperty.Value.Type, isRequired))
                        .FromJson();
                }
            }
        });
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

    private static string GetGraphQLTypeName(string openApiSchemaTypeName, bool required)
    {
        var typename = openApiSchemaTypeName switch
        {
            "string" => ScalarNames.String,
            "integer" => ScalarNames.Int,
            "boolean" => ScalarNames.Boolean,
            _ => openApiSchemaTypeName
        };

        var suffix = required ? "!" : string.Empty;
        return $"{typename}{suffix}";
    }

    public static string GetFieldName(string input)
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

        return sb.ToString();
    }

}
