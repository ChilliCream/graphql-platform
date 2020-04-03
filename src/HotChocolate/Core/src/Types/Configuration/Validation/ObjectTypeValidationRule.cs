using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Configuration.Validation
{
    /// <summary>
    /// Implements the object type validation defined in the spec.
    /// http://spec.graphql.org/draft/#sec-Objects.Type-Validation
    /// </summary>
    internal class ObjectTypeValidationRule : ISchemaValidationRule
    {
        public void Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects,
            IReadOnlySchemaOptions options,
            ICollection<ISchemaError> errors)
        {
            if (options.StrictValidation)
            {
                foreach (ObjectType objectType in typeSystemObjects.OfType<ObjectType>())
                {
                    ValidateImplementations(objectType, errors);

                    if (objectType.Fields.Count == 0 ||
                        objectType.Fields.All(t => t.IsIntrospectionField))
                    {
                        // TODO : Resources
                        errors.Add(SchemaErrorBuilder.New()
                            .SetMessage(
                                "The object type `{0}` has to at least define one field in " +
                                "order to be valid.",
                                objectType.Name)
                            .SetTypeSystemObject(objectType)
                            .AddSyntaxNode(objectType.SyntaxNode)
                            .SpecifiedBy("sec-Objects.Type-Validation")
                            .Build());
                    }

                    for (int i = 0; i < objectType.Fields.Count; i++)
                    {
                        ObjectField field = objectType.Fields[i];

                        if (!field.IsIntrospectionField && field.Name.Value.StartsWith("__"))
                        {
                            // TODO : Resources
                            errors.Add(SchemaErrorBuilder.New()
                                .SetMessage(
                                    "Field names starting with `__` are reserved for " +
                                    " the GraphQL specification.")
                                .SetTypeSystemObject(objectType)
                                .AddSyntaxNode(objectType.SyntaxNode)
                                .AddSyntaxNode(field.SyntaxNode)
                                .SetExtension("field", field)
                                .SpecifiedBy("sec-Objects.Type-Validation")
                                .Build());
                        }

                        for (int j = 0; j < field.Arguments.Count; j++)
                        {
                            Argument argument = field.Arguments[j];

                            if (argument.Name.Value.StartsWith("__"))
                            {
                                // TODO : Resources
                                errors.Add(SchemaErrorBuilder.New()
                                    .SetMessage(
                                        "Argument names starting with `__` are reserved for " +
                                        " the GraphQL specification.")
                                    .SetTypeSystemObject(objectType)
                                    .AddSyntaxNode(objectType.SyntaxNode)
                                    .AddSyntaxNode(field.SyntaxNode)
                                    .AddSyntaxNode(argument.SyntaxNode)
                                    .SetExtension("field", field)
                                    .SetExtension("argument", argument)
                                    .SpecifiedBy("sec-Objects.Type-Validation")
                                    .Build());
                            }
                        }
                    }
                }
            }
        }

        private static void ValidateImplementations(
            ObjectType type,
            ICollection<ISchemaError> errors)
        {
            if (type.Interfaces.Count > 0)
            {
                foreach (InterfaceType implementedType in type.Interfaces)
                {
                    ValidateImplementation(type, implementedType, errors);
                }
            }
        }

        // http://spec.graphql.org/draft/#IsValidImplementation()
        private static void ValidateImplementation(
            ObjectType type,
            InterfaceType implementedType,
            ICollection<ISchemaError> errors)
        {
            if (!IsFullyImplementingInterface(type, implementedType))
            {
                errors.Add(SchemaErrorBuilder.New()
                    .SetMessage(
                        "The object type must also declare all interfaces " +
                        "declared by implemented interfaces.")
                    .SetTypeSystemObject(type)
                    .AddSyntaxNode(type.SyntaxNode)
                    .AddSyntaxNode(implementedType.SyntaxNode)
                    .SetExtension("type", type)
                    .SetExtension("implementedType", implementedType)
                    .SpecifiedBy("sec-Objects.Type-Validation")
                    .Build());
            }

            foreach (InterfaceField implementedField in implementedType.Fields)
            {
                if (type.Fields.TryGetField(implementedField.Name, out ObjectField field))
                {
                    ValidateArguments(field, implementedField, errors);

                    if (!IsValidImplementationFieldType(field.Type, implementedField.Type))
                    {
                        errors.Add(SchemaErrorBuilder.New()
                            .SetMessage(
                                "Field `{0}` must return a type which is equal to " +
                                "or a sub‐type of (covariant) the return type `{1}` " +
                                "of the interface field.",
                                field.Name,
                                implementedField.Type.Print())
                            .SetTypeSystemObject(type)
                            .AddSyntaxNode(field.SyntaxNode)
                            .AddSyntaxNode(implementedField.SyntaxNode)
                            .SetExtension("field", field)
                            .SetExtension("implementedField", implementedField)
                            .SpecifiedBy("sec-Objects.Type-Validation")
                            .Build());
                    }
                }
                else
                {
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(
                            "The field `{0}` must be implement by object type `{1}`.",
                            implementedField.Name,
                            type.Print())
                        .SetTypeSystemObject(type)
                        .AddSyntaxNode(implementedField.SyntaxNode)
                        .SetExtension("implementedField", implementedField)
                        .SpecifiedBy("sec-Objects.Type-Validation")
                        .Build());
                }
            }
        }

        private static void ValidateArguments(
            ObjectField field,
            InterfaceField implementedField,
            ICollection<ISchemaError> errors)
        {
            var implArgs = implementedField.Arguments.ToDictionary(t => t.Name);

            foreach (Argument argument in field.Arguments)
            {
                if (implArgs.TryGetValue(
                    argument.Name, out Argument? implementedArgument))
                {
                    implArgs.Remove(argument.Name);
                    if (!argument.Type.IsEqualTo(implementedArgument.Type))
                    {
                        errors.Add(SchemaErrorBuilder.New()
                            .SetMessage(
                                "The named argument `{0}` on field `{1}` must accept " +
                                "the same type `{2}` (invariant) as that named argument on " +
                                "the interface `{3}`.",
                                argument.Name,
                                field.Name,
                                implementedArgument.Type.Print(),
                                implementedField.DeclaringType.Name)
                            .SetTypeSystemObject(argument.DeclaringType)
                            .AddSyntaxNode(argument.SyntaxNode)
                            .AddSyntaxNode(implementedArgument.SyntaxNode)
                            .SetExtension("argument", argument)
                            .SetExtension("implementedArgument", implementedArgument)
                            .SpecifiedBy("sec-Objects.Type-Validation")
                            .Build());
                    }
                }
                else if (argument.Type.IsNonNullType())
                {
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(
                            "The field `{0}` must only declare additional arguments to an " +
                            "implemented field that are nullable.",
                            field.Name)
                        .SetTypeSystemObject(argument.DeclaringType)
                        .AddSyntaxNode(field.SyntaxNode)
                        .AddSyntaxNode(implementedField.SyntaxNode)
                        .SetExtension("field", field)
                        .SetExtension("argument", argument)
                        .SetExtension("implementedField", implementedField)
                        .SpecifiedBy("sec-Objects.Type-Validation")
                        .Build());
                }
            }

            foreach (Argument missingArgument in implArgs.Values)
            {
                errors.Add(SchemaErrorBuilder.New()
                    .SetMessage(
                        "The argument `{0}` of the implemented field `{1}` must be defined. " +
                        "The field `{2}` must include an argument of the same name for " +
                        "every argument defined on the implemented field " +
                        "of the interface type `{3}`.",
                        missingArgument.Name,
                        field.Name,
                        field.Name,
                        implementedField.DeclaringType.Print())
                    .SetTypeSystemObject(field.DeclaringType)
                    .AddSyntaxNode(field.SyntaxNode)
                    .AddSyntaxNode(implementedField.SyntaxNode)
                    .SetExtension("field", field)
                    .SetExtension("implementedField", implementedField)
                    .SetExtension("missingArgument", missingArgument)
                    .SpecifiedBy("sec-Objects.Type-Validation")
                    .Build());
            }
        }

        private static bool IsFullyImplementingInterface(
            ObjectType type,
            InterfaceType implementedType)
        {
            foreach (InterfaceType interfaceType in implementedType.Interfaces)
            {
                if (!type.IsImplementing(interfaceType))
                {
                    return false;
                }
            }
            return true;
        }

        // http://spec.graphql.org/draft/#IsValidImplementationFieldType()
        private static bool IsValidImplementationFieldType(
            IOutputType fieldType,
            IOutputType implementedType)
        {
            if (fieldType.IsNonNullType())
            {
                fieldType = (IOutputType)fieldType.InnerType();

                if (implementedType.IsNonNullType())
                {
                    implementedType = (IOutputType)implementedType.InnerType();
                }

                return IsValidImplementationFieldType(fieldType, implementedType);
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

            if (fieldType is ObjectType objectType &&
                implementedType is UnionType unionType &&
                unionType.IsAssignableFrom(objectType))
            {
                return true;
            }

            if (fieldType is IComplexOutputType complexType &&
                implementedType is InterfaceType interfaceType &&
                complexType.IsImplementing(interfaceType))
            {
                return true;
            }

            return false;
        }
    }
}
