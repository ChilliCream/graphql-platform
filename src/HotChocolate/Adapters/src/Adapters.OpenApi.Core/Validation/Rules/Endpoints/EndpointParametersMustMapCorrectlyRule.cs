using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that the parameters of an endpoint must map to a proper variable / input object field.
/// </summary>
internal sealed class EndpointParametersMustMapCorrectlyRule: IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        List<OpenApiDefinitionValidationError>? errors = null;

        if (endpoint.BodyVariableName is not null && !TryGetVariable(endpoint, endpoint.BodyVariableName, out _))
        {
            errors ??= [];
            errors.Add(new OpenApiDefinitionValidationError(
                $"Body variable '${endpoint.BodyVariableName}' does not exist in the operation.",
                endpoint));
        }

        foreach (var routeParam in endpoint.RouteParameters)
        {
            if (!TryGetVariable(endpoint, routeParam.VariableName, out var variable))
            {
                errors ??= [];
                errors.Add(new OpenApiDefinitionValidationError(
                    $"Route parameter '{routeParam.Key}' references variable '${routeParam.VariableName}' which does not exist in the operation.",
                    endpoint));
                continue;
            }

            if (routeParam.InputObjectPath is { Length: > 0 } inputObjectPath
                && context.Schema is not null
                && !IsValidInputObjectPath(variable, inputObjectPath, context.Schema))
            {
                errors ??= [];
                errors.Add(new OpenApiDefinitionValidationError(
                    $"Route parameter '{routeParam.Key}' has an invalid input object path '{string.Join(".", inputObjectPath)}' for variable '${routeParam.VariableName}'.",
                    endpoint));
            }
        }

        foreach (var queryParam in endpoint.QueryParameters)
        {
            if (!TryGetVariable(endpoint, queryParam.VariableName, out var variable))
            {
                errors ??= [];
                errors.Add(new OpenApiDefinitionValidationError(
                    $"Query parameter '{queryParam.Key}' references variable '${queryParam.VariableName}' which does not exist in the operation.",
                    endpoint));
                continue;
            }

            if (queryParam.InputObjectPath is { Length: > 0 } inputObjectPath
                && context.Schema is not null
                && !IsValidInputObjectPath(variable, inputObjectPath, context.Schema))
            {
                errors ??= [];
                errors.Add(new OpenApiDefinitionValidationError(
                    $"Query parameter '{queryParam.Key}' has an invalid input object path '{string.Join(".", inputObjectPath)}' for variable '${queryParam.VariableName}'.",
                    endpoint));
            }
        }

        return errors is not { Count: > 0 }
            ? OpenApiDefinitionValidationResult.Success()
            : OpenApiDefinitionValidationResult.Failure(errors);
    }

    private static bool TryGetVariable(
        OpenApiEndpointDefinition endpoint,
        string variableName,
        [NotNullWhen(true)] out VariableDefinitionNode? variable)
    {
        variable = endpoint.OperationDefinition.VariableDefinitions
            .FirstOrDefault(x => x.Variable.Name.Value == variableName);

        return variable is not null;
    }

    private static bool IsValidInputObjectPath(
        VariableDefinitionNode variable,
        ImmutableArray<string> inputObjectPath,
        ISchemaDefinition schema)
    {
        var namedVariableType = variable.Type.NamedType().Name.Value;

        if (!schema.Types.TryGetType<IInputTypeDefinition>(namedVariableType, out var variableType))
        {
            return false;
        }

        var currentType = variableType.NamedType();

        foreach (var segment in inputObjectPath)
        {
            if (currentType is not IInputObjectTypeDefinition inputObject)
            {
                return false;
            }

            if (!inputObject.Fields.TryGetField(segment, out var inputField))
            {
                return false;
            }

            currentType = inputField.Type.NamedType();
        }

        return true;
    }
}
