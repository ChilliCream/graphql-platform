using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal static class VariableCoercionHelper
{
    public static bool TryCoerceVariableValues(
        IFeatureProvider context,
        ISchemaDefinition schema,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        JsonElement variableValues,
        [NotNullWhen(true)] out Dictionary<string, VariableValue>? coercedVariableValues,
        [NotNullWhen(false)] out IError? error)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(variableDefinitions);

        if (variableValues.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined))
        {
            throw new ArgumentException(
                "Variable values must be a JSON Object.",
                nameof(variableValues));
        }

        Utf8MemoryBuilder? memory = null;
        var hasVariables = variableValues.ValueKind is JsonValueKind.Object;
        coercedVariableValues = [];
        error = null;

        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var variableDefinition = variableDefinitions[i];
            var variableName = variableDefinition.Variable.Name.Value;
            var variableType = AssertInputType(schema, variableDefinition);
            JsonElement propertyValue = default;

            var hasValue = hasVariables && variableValues.TryGetProperty(variableName, out propertyValue);

            if (!hasValue && variableDefinition.DefaultValue is { Kind: not SyntaxKind.NullValue } defaultValue)
            {
                coercedVariableValues[variableName] = new VariableValue(variableName, variableType, defaultValue);
                continue;
            }

            if (!hasValue)
            {
                if (variableType.IsNonNullType())
                {
                    throw ExceptionHelper.NonNullVariableIsNull(variableDefinition);
                }

                // if we do not have any value, we will not create an entry to the
                // coerced variables.
                if (!hasValue)
                {
                    continue;
                }

                coercedVariableValues[variableName] =
                    new VariableValue(
                        variableName,
                        variableType,
                        NullValueNode.Default);
            }
            else
            {
                if (TryCoerceVariableValue(
                    context,
                    variableDefinition,
                    variableType,
                    propertyValue,
                    ref memory,
                    out var variableValue,
                    out error))
                {
                    coercedVariableValues[variableName] = variableValue.Value;
                }
                else
                {
                    coercedVariableValues = null;
                    return false;
                }
            }
        }

        memory?.Seal();
        return true;
    }

    private static bool TryCoerceVariableValue(
        IFeatureProvider context,
        VariableDefinitionNode variableDefinition,
        IInputType variableType,
        JsonElement value,
        ref Utf8MemoryBuilder? memory,
        [NotNullWhen(true)] out VariableValue? variableValue,
        [NotNullWhen(false)] out IError? error)
    {
        var coercion = new JsonVariableCoercion(context, ref memory);
        return coercion.TryCoerceVariableValue(
            variableDefinition.Variable.Name.Value,
            variableType,
            value,
            out variableValue,
            out error);
    }

    private static IInputType AssertInputType(
        ISchemaDefinition schema,
        VariableDefinitionNode variableDefinition)
    {
        if (schema.Types.TryGetType(variableDefinition.Type, out IInputType? type))
        {
            return type;
        }

        throw ExceptionHelper.VariableIsNotAnInputType(variableDefinition);
    }
}
