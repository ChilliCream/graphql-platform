using HotChocolate.OpenApi.Helpers;
using HotChocolate.OpenApi.Models;
using HotChocolate.Types;
using Microsoft.OpenApi.Models;
using InputField = HotChocolate.Skimmed.InputField;
using InputObjectType = HotChocolate.Skimmed.InputObjectType;
using IType = HotChocolate.Skimmed.IType;
using ScalarType = HotChocolate.Skimmed.ScalarType;

namespace HotChocolate.OpenApi.Pipeline;

internal sealed class InputTypeBuilderMiddleware : IOpenApiWrapperMiddleware
{
    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        foreach (var operation in context.GetMutationOperations())
        {
            AddInputType(context, operation.Value);
        }

        next.Invoke(context);
    }

    private static void AddInputType(OpenApiWrapperContext context, Operation operation)
    {
        var inputType = new InputObjectType(operation.GetInputTypeName());

        if (operation.Arguments == null) return;
        foreach (var argument in operation.Arguments)
        {
            if (argument.Parameter is { } parameter)
            {
                AddInputField(parameter.Name, context, parameter.Schema, inputType);
            }

            if (argument.RequestBody is { } requestBody)
            {
                var schema = requestBody.Content.FirstOrDefault().Value.Schema;
                if (schema is null) continue;

                AddInputField(requestBody.Content.First().Value.Schema.Reference.Id, context, schema, inputType);
            }
        }
        AddIfNecessary(context, inputType);
    }

    private static void AddIfNecessary(OpenApiWrapperContext context, InputObjectType inputObjectType)
    {
        if (context.SkimmedSchema.Types.ContainsName(inputObjectType.Name)) return;
        context.SkimmedSchema.Types.Add(inputObjectType);
    }

    private static void AddInputField(
        string fieldName,
        OpenApiWrapperContext context,
        OpenApiSchema schema,
        InputObjectType inputType)
    {
        var typeInfo = schema.GetSchemaTypeInfo();
        var possibleGraphQLName = OpenApiSchemaHelper.GetGraphQLTypeName(typeInfo.Name, typeInfo.Format);
        var isScalar = Scalars.IsBuiltIn(possibleGraphQLName);
        inputType.Fields.Add(isScalar
            ? new InputField(fieldName, new ScalarType(possibleGraphQLName))
            : new InputField(fieldName, CreateInputType(context, schema)));
    }

    private static IType CreateInputType(OpenApiWrapperContext context, OpenApiSchema schema)
    {
        var inputType = new InputObjectType($"{schema.Reference.Id}InputType");
        foreach (var schemaProperty in schema.Properties)
        {
            AddInputField(schemaProperty.Key, context, schemaProperty.Value, inputType);
        }

        foreach (var allOf in schema.AllOf)
        {
            foreach (var allOfProperty in allOf.Properties)
            {
                AddInputField(allOfProperty.Key,
                    context,
                    allOfProperty.Value,
                    inputType);
            }
        }

        AddIfNecessary(context, inputType);
        return inputType;
    }
}
