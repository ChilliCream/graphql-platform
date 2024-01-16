using HotChocolate.OpenApi.Helpers;
using HotChocolate.Skimmed;
using Microsoft.OpenApi.Models;
using InputField = HotChocolate.Skimmed.InputField;
using InputObjectType = HotChocolate.Skimmed.InputObjectType;
using IType = HotChocolate.Skimmed.IType;
using ScalarType = HotChocolate.Skimmed.ScalarType;

namespace HotChocolate.OpenApi.Pipeline;


/// <summary>
/// Parses all operations and creates input type where it's necessary
/// </summary>
internal sealed class CreateInputTypesMiddleware : IOpenApiWrapperMiddleware
{
    /// <inheritdoc />
    public void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next)
    {
        CreateQueryInputTypes(context);
        CreateMutationInputTypes(context);

        next.Invoke(context);
    }

    private static void CreateQueryInputTypes(OpenApiWrapperContext context)
    {
        foreach (var operation in context.GetQueryOperations())
        {
            foreach (var parameter in operation.Value.Parameters)
            {
                CreateInputTypeForNonScalar(context, parameter.Schema);
            }

            if (operation.Value.RequestBody is { } requestBody &&
                requestBody.Content.FirstOrDefault().Value.Schema is {} schema)
            {
                CreateInputTypeForNonScalar(context, schema);
            }
        }
    }

    private static void CreateInputTypeForNonScalar(OpenApiWrapperContext context, OpenApiSchema schema)
    {
        var typeInfo = context.GetSchemaTypeInfo(schema);

        if (!typeInfo.IsScalar)
        {
            CreateInputType(context, schema);
        }
    }

    private static void CreateMutationInputTypes(OpenApiWrapperContext context)
    {
        foreach (var operation in context.GetMutationOperations())
        {
            AddInputType(context, operation.Value);
        }
    }

    private static void AddInputType(OpenApiWrapperContext context, Operation operation)
    {
        var inputType = new InputObjectType(OpenApiNamingHelper.GetInputTypeName(operation.OperationId));

        foreach (var parameter in operation.Parameters)
        {
            AddInputField(parameter.Name, parameter.Required, context, parameter.Schema, inputType);
        }

        if (operation.RequestBody is { } requestBody &&
            requestBody.Content.FirstOrDefault().Value.Schema is {} schema)
        {
            AddFieldsToInputType(context, schema, inputType);
        }

        AddIfNecessary(context, inputType);
        context.OperationInputTypeLookup[operation.OperationId] = inputType;
    }

    private static void AddIfNecessary(OpenApiWrapperContext context, InputObjectType inputObjectType)
    {
        if (context.MutableSchema.Types.ContainsName(inputObjectType.Name))
        {
            return;
        }

        context.MutableSchema.Types.Add(inputObjectType);
    }

    private static void AddInputField(
        string fieldName,
        bool required,
        OpenApiWrapperContext context,
        OpenApiSchema schema,
        InputObjectType inputType)
    {
        var graphQLName = OpenApiNamingHelper.GetFieldName(fieldName);
        var typeInfo = context.GetSchemaTypeInfo(schema);

        IType type = typeInfo.IsScalar
            ? new ScalarType(typeInfo.GraphQLTypeName)
            : CreateInputType(context, schema);
        type = required ? new NonNullType(type) : type;

        inputType.Fields.Add(new InputField(graphQLName, type));
    }
    
    private static InputObjectType CreateInputType(OpenApiWrapperContext context, OpenApiSchema schema)
    {
        var inputType = new InputObjectType(OpenApiNamingHelper.GetInputTypeName(schema.Reference.Id));
        AddFieldsToInputType(context, schema, inputType);

        AddIfNecessary(context, inputType);
        return inputType;
    }

    private static void AddFieldsToInputType(
        OpenApiWrapperContext context, 
        OpenApiSchema schema, 
        InputObjectType inputType)
    {
        foreach (var schemaProperty in schema.Properties)
        {
            AddInputField(
                schemaProperty.Key,
                schema.Required.Contains(schemaProperty.Key),
                context,
                schemaProperty.Value,
                inputType);
        }

        foreach (var allOf in schema.AllOf)
        {
            foreach (var allOfProperty in allOf.Properties)
            {
                AddInputField(
                    allOfProperty.Key,
                    allOf.Required.Contains(allOfProperty.Key),
                    context,
                    allOfProperty.Value,
                    inputType);
            }
        }
    }
}
