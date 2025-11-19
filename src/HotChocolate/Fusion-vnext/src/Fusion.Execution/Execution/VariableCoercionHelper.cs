using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
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

                // if we do not have any value, we will not create an entry to the
                // coerced variables.
                if (!hasValue)
                {
                    continue;
                }

                coercedVariableValues[variableName] =
                    new VariableValue(variableName, variableType, NullValueNode.Default);
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
                    $"The variable value of type {value.GetType().Name} is not supported.");
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

        if (!ValidateValue(
            variableType,
            value,
            root,
            0,
            out error))
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

    private static bool ValidateValue(
        IInputType type,
        IValueNode value,
        Path path,
        int stack,
        [NotNullWhen(false)] out IError? error)
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
                if (!ValidateValue(elementType, listValue.Items[i], path.Append(i), stack, out error))
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

            var inputObjectType = (FusionInputObjectTypeDefinition)type;

            var oneOf = inputObjectType.IsOneOf;

            if (oneOf && objectValue.Fields.Count is 0)
            {
                error = ErrorBuilder.New()
                    .SetMessage("The OneOf Input Object `{0}` requires that exactly one field is supplied and that field must not be `null`. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.", inputObjectType.Name)
                    .SetCode(ErrorCodes.Execution.OneOfNoFieldSet)
                    .SetPath(path)
                    .Build();
                return false;
            }

            if (oneOf && objectValue.Fields.Count > 1)
            {
                error = ErrorBuilder.New()
                    .SetMessage("More than one field of the OneOf Input Object `{0}` is set. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.", inputObjectType.Name)
                    .SetCode(ErrorCodes.Execution.OneOfMoreThanOneFieldSet)
                    .SetPath(path)
                    .Build();
                return false;
            }

            var numberOfInputFields = inputObjectType.Fields.Count;

            var processedCount = 0;
            bool[]? processedBuffer = null;
            var processed = stack <= 256 && numberOfInputFields <= 32
                ? stackalloc bool[numberOfInputFields]
                : processedBuffer = ArrayPool<bool>.Shared.Rent(numberOfInputFields);

            if (processedBuffer is not null)
            {
                processed.Clear();
            }

            if (processedBuffer is null)
            {
                stack += numberOfInputFields;
            }

            try
            {
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

                    if (oneOf && field.Value.Kind is SyntaxKind.NullValue)
                    {
                        error = ErrorBuilder.New()
                            .SetMessage("`null` was set to the field `{0}`of the OneOf Input Object `{1}`. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.", field.Name, inputObjectType.Name)
                            .SetCode(ErrorCodes.Execution.OneOfFieldIsNull)
                            .SetPath(path)
                            .SetCoordinate(fieldDefinition.Coordinate)
                            .Build();
                        return false;
                    }

                    if (!ValidateValue(
                        fieldDefinition.Type,
                        field.Value,
                        path.Append(field.Name.Value),
                        stack,
                        out error))
                    {
                        return false;
                    }

                    processed[fieldDefinition.Index] = true;
                    processedCount++;
                }

                // If not all fields of the input object type were specified,
                // we have to check if any of the ones left out are non-null
                // and do not have a default value, and if so, raise an error.
                if (!oneOf && processedCount != numberOfInputFields)
                {
                    for (var i = 0; i < numberOfInputFields; i++)
                    {
                        if (!processed[i])
                        {
                            var field = inputObjectType.Fields[i];

                            if (field.Type.Kind == TypeKind.NonNull && field.DefaultValue is null)
                            {
                                error = ErrorBuilder.New()
                                    .SetMessage("The required input field `{0}` is missing.", field.Name)
                                    .SetPath(path.Append(field.Name))
                                    .SetExtension("field", field.Coordinate.ToString())
                                    .Build();
                                return false;
                            }
                        }
                    }
                }

                error = null;
                return true;
            }
            finally
            {
                if (processedBuffer is not null)
                {
                    ArrayPool<bool>.Shared.Return(processedBuffer);
                }
            }
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

        if (type is FusionEnumTypeDefinition enumType)
        {
            if (value is not (StringValueNode or EnumValueNode))
            {
                error = ErrorBuilder.New()
                    .SetMessage("The value `{0}` is not an enum value.", value.Value ?? "null")
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            if (!enumType.Values.ContainsName((string)value.Value!))
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
