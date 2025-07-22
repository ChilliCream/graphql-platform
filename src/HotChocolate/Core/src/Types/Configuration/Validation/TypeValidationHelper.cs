using HotChocolate.Types;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Configuration.Validation;

internal static class TypeValidationHelper
{
    public static void EnsureTypeHasFields(
        IComplexTypeDefinition type,
        ICollection<ISchemaError> errors)
    {
        if (type.Fields.Count == 0
            || type.Fields.All(t => t.IsIntrospectionField))
        {
            errors.Add(NeedsOneAtLeastField(type));
        }
    }

    public static void EnsureFieldDeprecationIsValid(
        IInputObjectTypeDefinition type,
        ICollection<ISchemaError> errors)
    {
        foreach (var field in type.Fields)
        {
            if (field.IsDeprecated && field.Type.IsNonNullType() && field.DefaultValue is null)
            {
                errors.Add(RequiredFieldCannotBeDeprecated(type, field));
            }
        }
    }

    public static void EnsureArgumentDeprecationIsValid(
        IComplexTypeDefinition type,
        ICollection<ISchemaError> errors)
    {
        foreach (var field in type.Fields)
        {
            foreach (var argument in field.Arguments)
            {
                if (argument.IsDeprecated && argument.Type.IsNonNullType() && argument.DefaultValue is null)
                {
                    errors.Add(RequiredArgumentCannotBeDeprecated(type, field, argument));
                }
            }
        }
    }

    public static void EnsureArgumentDeprecationIsValid(
        IDirectiveDefinition type,
        ICollection<ISchemaError> errors)
    {
        foreach (var argument in type.Arguments)
        {
            if (argument.IsDeprecated && argument.Type.IsNonNullType() && argument.DefaultValue is null)
            {
                errors.Add(RequiredArgumentCannotBeDeprecated(type, argument));
            }
        }
    }

    public static void EnsureTypeHasFields(
        InputObjectType type,
        ICollection<ISchemaError> errors)
    {
        if (type.Fields.Count == 0)
        {
            errors.Add(NeedsOneAtLeastField(type));
        }
    }

    public static void EnsureFieldNamesAreValid(
        IComplexTypeDefinition type,
        ICollection<ISchemaError> errors)
    {
        foreach (var field in type.Fields)
        {
            if (!field.IsIntrospectionField)
            {
                if (StartsWithTwoUnderscores(field.Name))
                {
                    errors.Add(TwoUnderscoresNotAllowedField(type, field));
                }

                foreach (var argument in field.Arguments)
                {
                    if (StartsWithTwoUnderscores(argument.Name))
                    {
                        errors.Add(
                            TwoUnderscoresNotAllowedOnArgument(
                                type,
                                field,
                                argument));
                    }
                }
            }
        }
    }

    public static void EnsureFieldNamesAreValid(
        InputObjectType type,
        ICollection<ISchemaError> errors)
    {
        foreach (var field in type.Fields)
        {
            if (StartsWithTwoUnderscores(field.Name))
            {
                errors.Add(TwoUnderscoresNotAllowedField(type, field));
            }
        }
    }

    public static void EnsureArgumentNamesAreValid(
        IDirectiveDefinition directiveDefinition,
        ICollection<ISchemaError> errors)
    {
        foreach (var argument in directiveDefinition.Arguments)
        {
            if (StartsWithTwoUnderscores(argument.Name))
            {
                errors.Add(TwoUnderscoresNotAllowedOnArgument(directiveDefinition, argument));
            }
        }
    }

    public static void EnsureInterfacesAreCorrectlyImplemented(
        IComplexTypeDefinition type,
        ICollection<ISchemaError> errors)
    {
        if (type.Implements.Count > 0)
        {
            foreach (var implementedType in type.Implements)
            {
                ValidateImplementation(type, implementedType, errors);
            }
        }
    }

    // https://spec.graphql.org/draft/#IsValidImplementation()
    private static void ValidateImplementation(
        IComplexTypeDefinition type,
        IInterfaceTypeDefinition implementedType,
        ICollection<ISchemaError> errors)
    {
        if (!IsFullyImplementingInterface(type, implementedType))
        {
            errors.Add(NotTransitivelyImplemented(type, implementedType));
        }

        foreach (var implementedField in implementedType.Fields)
        {
            if (type.Fields.TryGetField(implementedField.Name, out var field))
            {
                ValidateArguments(field, implementedField, errors);

                if (!IsValidImplementationFieldType(field.Type, implementedField.Type))
                {
                    errors.Add(InvalidFieldType(type, field, implementedField));
                }

                if (field.IsDeprecated && !implementedField.IsDeprecated)
                {
                    errors.Add(InvalidFieldDeprecation(
                        implementedType.Name,
                        implementedField,
                        type,
                        field));
                }
            }
            else
            {
                errors.Add(FieldNotImplemented(type, implementedField));
            }
        }
    }

    private static void ValidateArguments(
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField,
        ICollection<ISchemaError> errors)
    {
        var implArgs = implementedField.Arguments.ToDictionary(t => t.Name);

        foreach (var argument in field.Arguments)
        {
            if (implArgs.Remove(argument.Name, out var implementedArgument))
            {
                if (!argument.Type.IsStructurallyEqual(implementedArgument.Type))
                {
                    errors.Add(
                        InvalidArgumentType(
                            field,
                            implementedField,
                            argument,
                            implementedArgument));
                }
            }
            else if (argument.Type.IsNonNullType())
            {
                errors.Add(
                    AdditionalArgumentNotNullable(
                        field,
                        implementedField,
                        argument));
            }
        }

        foreach (var missingArgument in implArgs.Values)
        {
            errors.Add(
                ArgumentNotImplemented(
                    field,
                    implementedField,
                    missingArgument));
        }
    }

    private static bool IsFullyImplementingInterface(
        IComplexTypeDefinition type,
        IInterfaceTypeDefinition implementedType)
    {
        foreach (var interfaceType in implementedType.Implements)
        {
            if (!type.IsImplementing(interfaceType))
            {
                return false;
            }
        }

        return true;
    }

    // https://spec.graphql.org/draft/#IsValidImplementationFieldType()
    private static bool IsValidImplementationFieldType(
        IOutputType fieldType,
        IOutputType implementedType)
    {
        if (fieldType.IsNonNullType())
        {
            if (!implementedType.IsNonNullType())
            {
                return IsValidImplementationFieldType(
                    (IOutputType)fieldType.InnerType(),
                    implementedType);
            }

            return IsValidImplementationFieldType(
                (IOutputType)fieldType.InnerType(),
                (IOutputType)implementedType.InnerType());
        }

        if (implementedType.IsNonNullType())
        {
            return false;
        }

        if (fieldType.IsListType() && implementedType.IsListType())
        {
            return IsValidImplementationFieldType(
                (IOutputType)fieldType.ElementType(),
                (IOutputType)implementedType.ElementType());
        }

        if (ReferenceEquals(fieldType, implementedType))
        {
            return true;
        }

        if (fieldType is ObjectType objectType
            && implementedType.Kind is TypeKind.Union
            && implementedType.AsTypeDefinition().IsAssignableFrom(objectType))
        {
            return true;
        }

        if (fieldType is IComplexTypeDefinition complexType
            && implementedType.Kind is TypeKind.Interface
            && complexType.IsImplementing(implementedType.TypeName()))
        {
            return true;
        }

        return false;
    }

    private static bool StartsWithTwoUnderscores(string name)
    {
        if (name.Length < 2)
        {
            return false;
        }

        return name.AsSpan(0, 2).SequenceEqual("__");
    }
}
