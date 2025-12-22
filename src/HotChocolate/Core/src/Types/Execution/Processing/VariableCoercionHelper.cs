using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

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
        Dictionary<string, VariableValue> coercedValues)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(variableDefinitions);
        ArgumentNullException.ThrowIfNull(coercedValues);

        if (variableValues.ValueKind is JsonValueKind.Object)
        {
            throw new ArgumentException("variables must be a JSON Object", nameof(variableValues));
        }

        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var variableDefinition = variableDefinitions[i];
            var variableName = variableDefinition.Variable.Name.Value;
            var variableType = AssertInputType(schema, variableDefinition);

            var hasValue = variableValues.TryGetProperty(variableName, out var propertyValue);

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
                coercedValues[variableName] = CoerceVariableValue(variableDefinition, variableType, propertyValue);
            }
        }
    }

    private VariableValue CoerceVariableValue(
        VariableDefinitionNode variableDefinition,
        IInputType variableType,
        JsonElement inputValue)
    {
        var root = Path.Root.Append(variableDefinition.Variable.Name.Value);

        try
        {
            return new VariableValue(
                variableDefinition.Variable.Name.Value,
                variableType,
                _inputParser.ParseInputValue(inputValue, variableType, root),
                _inputFormatter.FormatValue(inputValue, variableType, root));
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
