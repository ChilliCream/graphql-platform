using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal static class VariableCoercionHelper
{
    public static bool TryCoerceVariableValues(
        ISchemaDefinition schema,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        IReadOnlyDictionary<string, object?> variableValues,
        [NotNullWhen(true)] out Dictionary<string, VariableValue>? coercedVariableValues,
        [NotNullWhen(false)] out IError? error)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(variableDefinitions);
        ArgumentNullException.ThrowIfNull(variableValues);

        coercedVariableValues = [];
        error = null;

        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var variableDefinition = variableDefinitions[i];
            var variableName = variableDefinition.Variable.Name.Value;
            var variableType = AssertInputType(schema, variableDefinition);

            var hasValue = variableValues.TryGetValue(variableName, out var value);

            if (!hasValue && variableDefinition.DefaultValue is { } defaultValue)
            {
                value = defaultValue.Kind is SyntaxKind.NullValue ? null : defaultValue;
                hasValue = true;
            }

            if (!hasValue || value is null || value is NullValueNode)
            {
                if (variableType.IsNonNullType())
                {
                    throw ExceptionHelper.NonNullVariableIsNull(variableDefinition);
                }

                // if we do not have any value we will not create an entry to the
                // coerced variables.
                if (!hasValue)
                {
                    continue;
                }

                coercedVariableValues[variableName] = new(variableName, variableType, NullValueNode.Default);
            }
            else if (value is IValueNode valueLiteral)
            {
                if (TryCoerceVariableValue(
                    variableDefinition,
                    variableType,
                    valueLiteral,
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
            else
            {
                throw new NotSupportedException(
                    $"The variable value of type {value?.GetType().Name} is not supported.");
            }
        }

        return true;
    }

    private static bool TryCoerceVariableValue(
        VariableDefinitionNode variableDefinition,
        IInputType variableType,
        IValueNode value,
        [NotNullWhen(true)] out VariableValue? variableValue,
        [NotNullWhen(false)] out IError? error)
    {
        var root = Path.Root.Append(variableDefinition.Variable.Name.Value);

        if (!ValidateValue(variableType, value, root, out error))
        {
            variableValue = null;
            return false;
        }

        variableValue = new VariableValue(
            variableDefinition.Variable.Name.Value,
            variableType,
            value);
        return true;
    }

    private static bool ValidateValue(IInputType type, IValueNode value, Path path, [NotNullWhen(false)] out IError? error)
    {
        if (type.Kind is TypeKind.NonNull)
        {
            if (value.Kind is SyntaxKind.NullValue)
            {
                error = ErrorBuilder.New()
                    .SetMessage("The value `{0}` is not a non-null value.", value)
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            type = (IInputType)type.InnerType();
        }

        if (value.Kind is SyntaxKind.NullValue)
        {
            error = null;
            return true;
        }

        if (type.Kind is TypeKind.List)
        {
            if (value is not ListValueNode listValue)
            {
                error = ErrorBuilder.New()
                    .SetMessage("The value `{0}` is not a list value.", value)
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            var elementType = (IInputType)type.ListType().ElementType;

            for (var i = 0; i < listValue.Items.Count; i++)
            {
                if (!ValidateValue(elementType, listValue.Items[i], path.Append(i), out error))
                {
                    return false;
                }
            }

            error = null;
            return true;
        }

        if (type.Kind is TypeKind.InputObject)
        {
            if (value is not ObjectValueNode objectValue)
            {
                error = ErrorBuilder.New()
                    .SetMessage("The value `{0}` is not an object value.", value)
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            var inputObjectType = (IInputObjectTypeDefinition)type;

            for (var i = 0; i < objectValue.Fields.Count; i++)
            {
                var field = objectValue.Fields[i];
                if (!inputObjectType.Fields.TryGetField(field.Name.Value, out var fieldDefinition))
                {
                    error = ErrorBuilder.New()
                        .SetMessage(
                            "The field `{0}` is not defined on the input object type `{1}`.",
                            field.Name.Value,
                            inputObjectType.Name)
                        .SetExtension("variable", $"{path}")
                        .Build();
                    return false;
                }

                if (!ValidateValue(fieldDefinition.Type, field.Value, path.Append(field.Name.Value), out error))
                {
                    return false;
                }
            }

            error = null;
            return true;
        }

        if (type is IScalarTypeDefinition scalarType)
        {
            if (!scalarType.IsInstanceOfType(value))
            {
                error = ErrorBuilder.New()
                    .SetMessage(
                        "The value `{0}` is not a valid value for the scalar type `{1}`.",
                        value,
                        scalarType.Name)
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            error = null;
            return true;
        }

        if (type is IEnumTypeDefinition enumType)
        {
            if (value is not StringValueNode stringValue)
            {
                error = ErrorBuilder.New()
                    .SetMessage("The value `{0}` is not an enum value.", value.Value ?? "null")
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            if (!enumType.Values.ContainsName(stringValue.Value))
            {
                error = ErrorBuilder.New()
                    .SetMessage("The value `{0}` is not a valid value for the enum type `{1}`.", value.Value ?? "null", enumType.Name)
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            error = null;
            return true;
        }

        throw new NotSupportedException(
            $"The type `{type.FullTypeName()}` is not a valid input type.");
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
