using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

// TODO : File Upload Rewrite
// return new FileReferenceNode(
//     fileValueNode.Value.OpenReadStream,
//     fileValueNode.Value.Name,
//     fileValueNode.Value.ContentType);

internal static class VariableCoercionHelper
{
    public static bool TryCoerceVariableValues(
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
                    variableDefinition,
                    variableType,
                    propertyValue,
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

        return true;
    }

    private static bool TryCoerceVariableValue(
        VariableDefinitionNode variableDefinition,
        IInputType variableType,
        JsonElement value,
        [NotNullWhen(true)] out VariableValue? variableValue,
        [NotNullWhen(false)] out IError? error)
    {
        var root = Path.Root.Append(variableDefinition.Variable.Name.Value);
        var parser = new JsonValueParser();
        var valueLiteral = parser.Parse(value);

        if (!ValidateValue(
            variableType,
            valueLiteral,
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
            valueLiteral);
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
                // TODO : resources
                error = ErrorBuilder.New()
                    .SetMessage("The OneOf Input Object `{0}` requires that exactly one field is supplied and that field must not be `null`. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.", inputObjectType.Name)
                    .SetCode(ErrorCodes.Execution.OneOfNoFieldSet)
                    .SetPath(path)
                    .Build();
                return false;
            }

            if (oneOf && objectValue.Fields.Count > 1)
            {
                // TODO : resources
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
                        // TODO : resources
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
                        // TODO : resources
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
            if (!scalarType.IsValueCompatible(value))
            {
                // TODO : resources
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
                // TODO : resources
                error = ErrorBuilder.New()
                    .SetMessage("The value `{0}` is not an enum value.", value.Value ?? "null")
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            if (!enumType.Values.ContainsName((string)value.Value!))
            {
                // TODO : resources
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
