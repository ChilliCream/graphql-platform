using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using static HotChocolate.Fusion.FusionUtilitiesResources;

namespace HotChocolate.Fusion.Validators;

/// <summary>
/// Validates the constant arguments supplied to a selected field against the field's argument
/// definitions: each argument must be defined on the field, its value must be compatible with the
/// argument type, and every required argument must be supplied.
/// </summary>
public static class ConstantArgumentValidator
{
    public static void Validate(
        IReadOnlyList<ArgumentNode> arguments,
        IOutputFieldDefinition field,
        string fieldName,
        List<string> errors)
    {
        var provided = new HashSet<string>(StringComparer.Ordinal);

        foreach (var argument in arguments)
        {
            if (!field.Arguments.TryGetField(argument.Name.Value, out var argumentDefinition))
            {
                errors.Add(
                    string.Format(
                        ConstantArgumentValidator_ArgumentDoesNotExist,
                        argument.Name.Value,
                        fieldName));

                continue;
            }

            provided.Add(argument.Name.Value);

            if (!IsCompatible(argument.Value, argumentDefinition.Type))
            {
                errors.Add(
                    string.Format(
                        ConstantArgumentValidator_ValueIncompatible,
                        argument.Name.Value,
                        fieldName,
                        argumentDefinition.Type.ToTypeNode().Print(indented: false)));
            }
        }

        foreach (var argumentDefinition in field.Arguments.AsEnumerable())
        {
            if (argumentDefinition.Type.IsNonNullType()
                && argumentDefinition.DefaultValue is null
                && !provided.Contains(argumentDefinition.Name))
            {
                errors.Add(
                    string.Format(
                        ConstantArgumentValidator_MissingRequiredArgument,
                        argumentDefinition.Name,
                        fieldName));
            }
        }
    }

    private static bool IsCompatible(IValueNode value, IType type)
    {
        if (type.IsNonNullType())
        {
            if (value is NullValueNode)
            {
                return false;
            }

            return IsCompatible(value, type.NullableType());
        }

        if (value is NullValueNode)
        {
            return true;
        }

        if (type.IsListType())
        {
            if (value is ListValueNode listValue)
            {
                return listValue.Items.All(item => IsCompatible(item, type.ElementType()));
            }

            // A single value is coerced into a list of one element.
            return IsCompatible(value, type.ElementType());
        }

        switch (type.AsTypeDefinition())
        {
            case IEnumTypeDefinition enumType:
                return value is EnumValueNode enumValue && enumType.Values.ContainsName(enumValue.Value);

            case IInputObjectTypeDefinition inputObjectType:
                return IsCompatibleObject(value, inputObjectType);

            case IScalarTypeDefinition scalarType:
                return IsCompatibleScalar(value, scalarType);

            default:
                return false;
        }
    }

    private static bool IsCompatibleObject(IValueNode value, IInputObjectTypeDefinition inputObjectType)
    {
        if (value is not ObjectValueNode objectValue)
        {
            return false;
        }

        var provided = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in objectValue.Fields)
        {
            if (!inputObjectType.Fields.TryGetField(field.Name.Value, out var inputField))
            {
                return false;
            }

            provided.Add(field.Name.Value);

            if (!IsCompatible(field.Value, inputField.Type))
            {
                return false;
            }
        }

        foreach (var inputField in inputObjectType.Fields.AsEnumerable())
        {
            if (inputField.Type.IsNonNullType()
                && inputField.DefaultValue is null
                && !provided.Contains(inputField.Name))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCompatibleScalar(IValueNode value, IScalarTypeDefinition scalarType)
    {
        // Built-in scalars get a literal-kind check. Custom scalars are opaque during composition,
        // so any non-null constant literal is accepted; execution performs full coercion.
        return scalarType.Name switch
        {
            "Int" => value is IntValueNode,
            "Float" => value is FloatValueNode or IntValueNode,
            "Boolean" => value is BooleanValueNode,
            "String" or "ID" => value is StringValueNode,
            _ => true
        };
    }
}
