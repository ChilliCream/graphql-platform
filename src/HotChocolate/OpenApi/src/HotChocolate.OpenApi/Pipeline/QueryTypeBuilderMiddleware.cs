using System.Text.Json;
using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Models;
using HotChocolate.Resolvers;
using HotChocolate.Skimmed;
using InputField = HotChocolate.Skimmed.InputField;
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

            var outputField = new OutputField(OpenApiNamingHelper.GetFieldName(operation.Value.OperationId))
            {
                Type = type
            };

            ObjectTypeHelper.CreateType(context, type.NamedType().Name, schema.GetTypeSchema());

            queryType.Fields.Add(outputField);

            AddArguments(operation, outputField);

            outputField.ContextData["resolver"] = new Func<IResolverContext, Task<JsonElement>>(async ctx =>
            {
                var resolver = OperationResolverHelper.CreateResolverFunc(operation.Value);
                return await resolver.Invoke(ctx);
            });
        }

        context.SkimmedSchema.QueryType = queryType;
    }



    private static void AddArguments(KeyValuePair<string, Operation> operation, OutputField outputField)
    {
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
