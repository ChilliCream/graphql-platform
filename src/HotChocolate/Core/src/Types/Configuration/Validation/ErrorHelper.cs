using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Configuration.Validation
{
    internal static class ErrorHelper
    {
        private const string _interfaceTypeValidation = "sec-Interfaces.Type-Validation";
        private const string _objectTypeValidation = "sec-Objects.Type-Validation";
        private const string _interface = "interface";
        private const string _object = "object";

        public static ISchemaError NeedsOneAtLeastField(IComplexOutputType type)
        {
            bool isInterface = type.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "The {0} type `{1}` has to at least define one field in " +
                    "order to be valid.",
                    isInterface ? _interface : _object,
                    type.Name)
                .SetType(type)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        public static ISchemaError TwoUnderscoresNotAllowedField(
            IComplexOutputType type,
            IOutputField field)
        {
            bool isInterface = type.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "Field names starting with `__` are reserved for " +
                    "the GraphQL specification.")
                .SetType(type)
                .SetField(field)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        public static ISchemaError TwoUnderscoresNotAllowedOnArgument(
            IComplexOutputType type,
            IOutputField field,
            IInputField argument)
        {
            bool isInterface = type.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "Argument names starting with `__` are reserved for " +
                    " the GraphQL specification.")
                .SetType(type)
                .SetField(field)
                .SetArgument(argument)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        public static ISchemaError NotTransitivelyImplemented(
            IComplexOutputType type,
            IComplexOutputType implementedType)
        {
            bool isInterface = type.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "The {0} type must also declare all interfaces " +
                    "declared by implemented interfaces.",
                    isInterface ? _interface : _object)
                .SetType(type)
                .SetImplementedType(implementedType)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        public static ISchemaError InvalidFieldType(
            IComplexOutputType type,
            IOutputField field,
            IOutputField implementedField)
        {
            bool isInterface = type.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "Field `{0}` must return a type which is equal to " +
                    "or a sub‐type of (covariant) the return type `{1}` " +
                    "of the interface field.",
                    field.Name,
                    implementedField.Type.Print())
                .SetType(type)
                .SetField(field)
                .SetImplementedField(implementedField)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        public static ISchemaError FieldNotImplemented(
            IComplexOutputType type,
            IOutputField implementedField)
        {
            bool isInterface = type.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "The field `{0}` must be implement by {1} type `{2}`.",
                    implementedField.Name,
                    isInterface ? _interface : _object,
                    type.Print())
                .SetType(type)
                .SetImplementedField(implementedField)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        public static ISchemaError InvalidArgumentType(
            IOutputField field,
            IOutputField implementedField,
            IInputField argument,
            IInputField implementedArgument)
        {
            bool isInterface = field.DeclaringType.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "The named argument `{0}` on field `{1}` must accept " +
                    "the same type `{2}` (invariant) as that named argument on " +
                    "the interface `{3}`.",
                    argument.Name,
                    field.Name,
                    implementedArgument.Type.Print(),
                    implementedField.DeclaringType.Name)
                .SetType(field.DeclaringType)
                .SetArgument(argument)
                .SetImplementedArgument(implementedArgument)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        public static ISchemaError AdditionalArgumentNotNullable(
            IOutputField field,
            IOutputField implementedField,
            IInputField argument)
        {
            bool isInterface = field.DeclaringType.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "The field `{0}` must only declare additional arguments to an " +
                    "implemented field that are nullable.",
                    field.Name)
                .SetType(field.DeclaringType)
                .SetField(field)
                .SetImplementedField(implementedField)
                .SetArgument(argument)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        public static ISchemaError ArgumentNotImplemented(
            IOutputField field,
            IOutputField implementedField,
            IInputField missingArgument)
        {
            bool isInterface = field.DeclaringType.Kind == TypeKind.Interface;

            return SchemaErrorBuilder.New()
                .SetMessage(
                    "The argument `{0}` of the implemented field `{1}` must be defined. " +
                    "The field `{2}` must include an argument of the same name for " +
                    "every argument defined on the implemented field " +
                    "of the interface type `{3}`.",
                    missingArgument.Name,
                    field.Name,
                    field.Name,
                    implementedField.DeclaringType.Print())
                .SetType(field.DeclaringType)
                .SetField(field)
                .SetImplementedField(implementedField)
                .AddSyntaxNode(missingArgument.SyntaxNode)
                .SetExtension("missingArgument", missingArgument)
                .SetSpecifiedBy(isInterface)
                .Build();
        }

        private static ISchemaErrorBuilder SetType(
            this ISchemaErrorBuilder errorBuilder,
            IComplexOutputType type) =>
            errorBuilder
                .AddSyntaxNode(type.SyntaxNode)
                .SetTypeSystemObject((TypeSystemObjectBase)type);

        private static ISchemaErrorBuilder SetField(
            this ISchemaErrorBuilder errorBuilder,
            IField field,
            string name = "field") =>
            errorBuilder
                .AddSyntaxNode(field.SyntaxNode)
                .SetExtension(name, field);

        private static ISchemaErrorBuilder SetArgument(
            this ISchemaErrorBuilder errorBuilder,
            IInputField field) =>
            errorBuilder.SetField(field, "argument");

        private static ISchemaErrorBuilder SetImplementedType(
            this ISchemaErrorBuilder errorBuilder,
            IComplexOutputType type) =>
            errorBuilder
                .AddSyntaxNode(type.SyntaxNode)
                .SetExtension("implementedType", type);

        private static ISchemaErrorBuilder SetImplementedField(
            this ISchemaErrorBuilder errorBuilder,
            IOutputField field) =>
            errorBuilder.SetField(field, "implementedField");

        private static ISchemaErrorBuilder SetImplementedArgument(
            this ISchemaErrorBuilder errorBuilder,
            IInputField field) =>
            errorBuilder.SetField(field, "implementedArgument");

        private static ISchemaErrorBuilder SetSpecifiedBy(
            this ISchemaErrorBuilder errorBuilder,
            bool isInterface) =>
            errorBuilder
                .SpecifiedBy(_interfaceTypeValidation, isInterface)
                .SpecifiedBy(_objectTypeValidation, !isInterface);
    }
}
