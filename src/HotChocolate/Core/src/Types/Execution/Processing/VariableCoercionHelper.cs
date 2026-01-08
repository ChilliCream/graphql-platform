using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Properties.Resources;

namespace HotChocolate.Execution.Processing;

internal sealed class VariableCoercionHelper
{
    private readonly InputParser _inputParser;
    private readonly InputFormatter _inputFormatter;

    public VariableCoercionHelper(InputParser inputParser, InputFormatter inputFormatter)
    {
        ArgumentNullException.ThrowIfNull(inputParser);
        ArgumentNullException.ThrowIfNull(inputFormatter);

        _inputParser = inputParser;
        _inputFormatter = inputFormatter;
    }

    public void CoerceVariableValues(
        ISchemaDefinition schema,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        JsonElement variableValues,
        Dictionary<string, VariableValue> coercedValues,
        IFeatureProvider context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(variableDefinitions);
        ArgumentNullException.ThrowIfNull(coercedValues);

        if (variableValues.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined))
        {
            throw new ArgumentException(
                VariableCoercionHelper_CoerceVariableValues_VariablesMustBeObject,
                nameof(variableValues));
        }

        var hasVariables = variableValues.ValueKind is JsonValueKind.Object;

        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var variableDefinition = variableDefinitions[i];
            var variableName = variableDefinition.Variable.Name.Value;
            var variableType = AssertInputType(schema, variableDefinition);
            JsonElement propertyValue = default;

            var hasValue = hasVariables && variableValues.TryGetProperty(variableName, out propertyValue);

            if (!hasValue && variableDefinition.DefaultValue is { Kind: not SyntaxKind.NullValue } defaultValue)
            {
                var runtimeValue = _inputParser.ParseLiteral(defaultValue, variableType);
                coercedValues[variableName] = new VariableValue(variableName, variableType, runtimeValue, defaultValue);
                continue;
            }

            if (!hasValue)
            {
                if (variableType.IsNonNullType())
                {
                    throw ThrowHelper.NonNullVariableIsNull(variableDefinition);
                }

                // if we do not have any value we will not create an entry to the
                // coerced variables.
                if (!hasValue)
                {
                    continue;
                }

                coercedValues[variableName] =
                    new VariableValue(
                        variableName,
                        variableType,
                        null,
                        NullValueNode.Default);
            }
            else
            {
                coercedValues[variableName] =
                    CoerceVariableValue(
                        variableDefinition,
                        variableType,
                        propertyValue,
                        context);
            }
        }
    }

    private VariableValue CoerceVariableValue(
        VariableDefinitionNode variableDefinition,
        IInputType variableType,
        JsonElement inputValue,
        IFeatureProvider context)
    {
        var root = Path.Root.Append(variableDefinition.Variable.Name.Value);

        try
        {
            var runtimeValue = _inputParser.ParseInputValue(inputValue, variableType, context, path: root);
            var valueLiteral = _inputFormatter.FormatValue(runtimeValue, variableType, root);

            return new VariableValue(
                variableDefinition.Variable.Name.Value,
                variableType,
                runtimeValue,
                valueLiteral);
        }
        catch (GraphQLException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw ThrowHelper.VariableValueInvalidType(variableDefinition, ex);
        }
    }

    private static IInputType AssertInputType(
        ISchemaDefinition schema,
        VariableDefinitionNode variableDefinition)
    {
        if (schema.Types.TryGetType(variableDefinition.Type, out IInputType? type))
        {
            return type;
        }

        throw ThrowHelper.VariableIsNotAnInputType(variableDefinition);
    }
}
