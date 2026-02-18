using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Properties.Resources;

namespace HotChocolate.Execution.Processing;

internal sealed class VariableCoercionHelper
{
    private readonly InputParser _inputParser;

    public VariableCoercionHelper(InputParser inputParser)
    {
        ArgumentNullException.ThrowIfNull(inputParser);
        _inputParser = inputParser;
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

        try
        {
            CoerceVariableValuesInternal(schema, variableDefinitions, variableValues, coercedValues, context);
        }
        finally
        {
            var memoryBuilder = context.Features.Get<Utf8MemoryBuilder>();
            if (memoryBuilder is not null)
            {
                memoryBuilder.Seal();
                context.Features.Set<Utf8MemoryBuilder>(null);
            }
        }
    }

    private void CoerceVariableValuesInternal(
        ISchemaDefinition schema,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        JsonElement variableValues,
        Dictionary<string, VariableValue> coercedValues,
        IFeatureProvider context)
    {
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
            var valueLiteral = CoerceInputLiteral(inputValue, variableType, context, depth: 0);

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

    private IValueNode CoerceInputLiteral(
        JsonElement inputValue,
        IInputType type,
        IFeatureProvider context,
        int depth)
    {
        if (depth > 64)
        {
            throw new InvalidOperationException("The input value is to deep.");
        }

        if (inputValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return NullValueNode.Default;
        }

        switch (type.Kind)
        {
            case TypeKind.Scalar:
                return Unsafe.As<ScalarType>(type).InputValueToLiteral(inputValue, context);

            case TypeKind.Enum:
                if (inputValue.ValueKind is not JsonValueKind.String)
                {
                    throw new InvalidOperationException();
                }

                return new EnumValueNode(inputValue.GetString()!);

            case TypeKind.InputObject:
                if (inputValue.ValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidOperationException();
                }

                var inputObjectType = (InputObjectType)type;
                var fields = new List<ObjectFieldNode>();
                var processedFields = StringSetPool.Shared.Rent();

                try
                {
                    foreach (var property in inputValue.EnumerateObject())
                    {
                        if (!inputObjectType.Fields.TryGetField(property.Name, out var field))
                        {
                            continue;
                        }

                        processedFields.Add(property.Name);

                        if (property.Value.ValueKind is JsonValueKind.Null)
                        {
                            fields.Add(new ObjectFieldNode(field.Name, NullValueNode.Default));
                        }
                        else
                        {
                            var value = CoerceInputLiteral(property.Value, field.Type, context, depth + 1);
                            fields.Add(new ObjectFieldNode(field.Name, value));
                        }
                    }

                    foreach (var field in inputObjectType.Fields)
                    {
                        if (field is { IsOptional: false, DefaultValue: not (null or NullValueNode) } && !processedFields.Contains(field.Name))
                        {
                            fields.Add(new ObjectFieldNode(field.Name, field.DefaultValue));
                        }
                    }

                    return new ObjectValueNode(fields);
                }
                finally
                {
                    StringSetPool.Shared.Return(processedFields);
                }

            case TypeKind.List:
                if (inputValue.ValueKind is not JsonValueKind.Array)
                {
                    throw new InvalidOperationException();
                }

                var items = new List<IValueNode>();
                var elementType = type.ElementType().EnsureInputType();
                var elementDepth = depth + 1;

                foreach (var item in inputValue.EnumerateArray())
                {
                    items.Add(CoerceInputLiteral(item, elementType, context, elementDepth));
                }

                return new ListValueNode(items);

            case TypeKind.NonNull:
                return CoerceInputLiteral(inputValue, type.InnerType().EnsureInputType(), context, depth);

            default:
                throw new NotSupportedException();
        }
    }
}
