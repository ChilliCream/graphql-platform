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
        List<string> errors)
    {
        var fieldCoordinate = field.Coordinate.ToString();
        var provided = new HashSet<string>(StringComparer.Ordinal);

        foreach (var argument in arguments)
        {
            if (!field.Arguments.TryGetField(argument.Name.Value, out var argumentDefinition))
            {
                errors.Add(
                    string.Format(
                        ConstantArgumentValidator_ArgumentDoesNotExist,
                        argument.Name.Value,
                        fieldCoordinate));

                continue;
            }

            if (!provided.Add(argument.Name.Value))
            {
                errors.Add(
                    string.Format(
                        ConstantArgumentValidator_DuplicateArgument,
                        argument.Name.Value,
                        fieldCoordinate));

                continue;
            }

            ReportDuplicateInputObjectFields(argument.Value, fieldCoordinate, errors);

            if (ContainsVariable(argument.Value))
            {
                errors.Add(
                    string.Format(
                        ConstantArgumentValidator_ArgumentMustBeConstant,
                        argument.Name.Value,
                        fieldCoordinate));

                continue;
            }

            if (!IsCompatible(argument.Value, argumentDefinition.Type))
            {
                errors.Add(
                    string.Format(
                        ConstantArgumentValidator_ValueIncompatible,
                        argument.Name.Value,
                        fieldCoordinate,
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
                        fieldCoordinate));
            }
        }
    }

    // A field selection map argument must be a constant; variables are never permitted, including
    // when nested inside a list or input object value.
    private static bool ContainsVariable(IValueNode value)
        => value switch
        {
            VariableNode => true,
            ListValueNode list => list.Items.Any(ContainsVariable),
            ObjectValueNode obj => obj.Fields.Any(field => ContainsVariable(field.Value)),
            _ => false
        };

    // Reports input object fields whose name appears more than once within the same object value
    // (recursively). The parser accepts duplicate names, so uniqueness is enforced here.
    private static void ReportDuplicateInputObjectFields(
        IValueNode value,
        string fieldCoordinate,
        List<string> errors)
    {
        switch (value)
        {
            case ObjectValueNode objectValue:
                var seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (var field in objectValue.Fields)
                {
                    if (!seen.Add(field.Name.Value))
                    {
                        errors.Add(
                            string.Format(
                                ConstantArgumentValidator_DuplicateInputField,
                                field.Name.Value,
                                fieldCoordinate));
                    }

                    ReportDuplicateInputObjectFields(field.Value, fieldCoordinate, errors);
                }

                break;

            case ListValueNode listValue:
                foreach (var item in listValue.Items)
                {
                    ReportDuplicateInputObjectFields(item, fieldCoordinate, errors);
                }

                break;
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

        return type.AsTypeDefinition() switch
        {
            IEnumTypeDefinition enumType
                => value is EnumValueNode enumValue && enumType.Values.ContainsName(enumValue.Value),
            IInputObjectTypeDefinition inputObjectType
                => IsCompatibleObject(value, inputObjectType),
            IScalarTypeDefinition scalarType
                => IsCompatibleScalar(value, scalarType),
            _ => false
        };
    }

    private static bool IsCompatibleObject(IValueNode value, IInputObjectTypeDefinition inputObjectType)
    {
        if (value is not ObjectValueNode objectValue)
        {
            return false;
        }

        // Tracks which fields are present for the required-field check below; duplicate field names
        // are reported separately by ReportDuplicateInputObjectFields.
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
            SpecScalarNames.Int.Name => value is IntValueNode,
            SpecScalarNames.Float.Name => value is FloatValueNode or IntValueNode,
            SpecScalarNames.Boolean.Name => value is BooleanValueNode,
            SpecScalarNames.String.Name => value is StringValueNode,
            SpecScalarNames.ID.Name => value is StringValueNode or IntValueNode,
            _ => true
        };
    }
}
