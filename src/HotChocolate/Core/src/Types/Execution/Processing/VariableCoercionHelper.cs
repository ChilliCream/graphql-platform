using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
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

            if (!hasValue && variableDefinition.DefaultValue is { } defaultValue)
            {
                var runtimeValue = _inputParser.ParseLiteral(
                    defaultValue,
                    variableType,
                    path: Path.Root.Append(variableName));
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
                continue;
            }

            coercedValues[variableName] =
                CoerceVariableValue(
                    variableDefinition,
                    variableType,
                    propertyValue,
                    context);
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

    private static IValueNode CoerceInputLiteral(
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

                // the runtime value is coerced first, so the raw name is known unless it is escaped.
                var utf8Name = JsonMarshal.GetRawUtf8Value(inputValue)[1..^1];
                return ((EnumType)type).Values.TryGetValue(utf8Name, out var enumValue)
                    ? new EnumValueNode(enumValue.Name)
                    : new EnumValueNode(inputValue.GetString()!);

            case TypeKind.InputObject:
                if (inputValue.ValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidOperationException();
                }

                var inputFields = ((InputObjectType)type).Fields;
                var fields = new List<ObjectFieldNode>();
                var processed = ArrayPool<bool>.Shared.Rent(inputFields.Count);
                processed.AsSpan(0, inputFields.Count).Clear();

                try
                {
                    foreach (var property in inputValue.EnumerateObject())
                    {
                        if (!InputParser.TryGetField(inputFields, property, out var field))
                        {
                            continue;
                        }

                        processed[field.Index] = true;

                        if (property.Value.ValueKind is JsonValueKind.Null)
                        {
                            fields.Add(new ObjectFieldNode(null, field.NameNode, NullValueNode.Default));
                        }
                        else
                        {
                            var value = CoerceInputLiteral(property.Value, field.Type, context, depth + 1);
                            fields.Add(new ObjectFieldNode(null, field.NameNode, value));
                        }
                    }

                    for (var i = 0; i < inputFields.Count; i++)
                    {
                        var field = inputFields[i];

                        if (!processed[i] && field is { IsOptional: false, DefaultValue: not (null or NullValueNode) })
                        {
                            fields.Add(new ObjectFieldNode(null, field.NameNode, field.DefaultValue));
                        }
                    }

                    return new ObjectValueNode(fields);
                }
                finally
                {
                    ArrayPool<bool>.Shared.Return(processed);
                }

            case TypeKind.List:
                var items = new List<IValueNode>();
                var elementType = type.ElementType().EnsureInputType();
                var elementDepth = depth + 1;

                if (inputValue.ValueKind is JsonValueKind.Array)
                {
                    foreach (var item in inputValue.EnumerateArray())
                    {
                        items.Add(CoerceInputLiteral(item, elementType, context, elementDepth));
                    }
                }
                else
                {
                    items.Add(CoerceInputLiteral(inputValue, elementType, context, elementDepth));
                }

                return new ListValueNode(items);

            case TypeKind.NonNull:
                return CoerceInputLiteral(inputValue, type.InnerType().EnsureInputType(), context, depth);

            default:
                throw new NotSupportedException();
        }
    }
}
