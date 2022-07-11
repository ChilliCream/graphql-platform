using System.Globalization;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Configuration.Validation;

internal static class ErrorHelper
{
    private const string _interfaceTypeValidation = "sec-Interfaces.Type-Validation";
    private const string _objectTypeValidation = "sec-Objects.Type-Validation";
    private const string _inputObjectTypeValidation = "sec-Input-Objects.Type-Validation";
    private const string _directiveValidation = "sec-Type-System.Directives.Validation";

    public static ISchemaError NeedsOneAtLeastField(INamedType type)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "The {0} type `{1}` has to at least define one field in " +
                "order to be valid.",
                type.Kind.ToString().ToLowerInvariant(),
                type.Name)
            .SetType(type)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedField(
        INamedType type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "Field names starting with `__` are reserved for " +
                "the GraphQL specification.")
            .SetType(type)
            .SetField(field)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedOnArgument(
        IComplexOutputType type,
        IOutputField field,
        IInputField argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "Argument names starting with `__` are reserved for " +
                " the GraphQL specification.")
            .SetType(type)
            .SetField(field)
            .SetArgument(argument)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedOnArgument(
        DirectiveType type,
        IInputField argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "Argument names starting with `__` are reserved for " +
                " the GraphQL specification.")
            .SetDirective(type)
            .SetArgument(argument)
            .SetSpecifiedBy(TypeKind.Directive)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedOnDirectiveName(DirectiveType type)
        => SchemaErrorBuilder.New()
            .SetMessage("Names starting with `__` are reserved for the GraphQL specification.")
            .SetDirective(type)
            .SetSpecifiedBy(TypeKind.Directive)
            .Build();

    public static ISchemaError NotTransitivelyImplemented(
        IComplexOutputType type,
        IComplexOutputType implementedType)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "The {0} type must also declare all interfaces " +
                "declared by implemented interfaces.",
                type.Kind.ToString().ToLowerInvariant())
            .SetType(type)
            .SetImplementedType(implementedType)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError InvalidFieldType(
        IComplexOutputType type,
        IOutputField field,
        IOutputField implementedField)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "Field `{0}` must return a type which is equal to " +
                "or a sub‐type of (covariant) the return type `{1}` " +
                "of the interface field.",
                field.Name,
                implementedField.Type.Print())
            .SetType(type)
            .SetField(field)
            .SetImplementedField(implementedField)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError FieldNotImplemented(
        IComplexOutputType type,
        IOutputField implementedField)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "The field `{0}` must be implement by {1} type `{2}`.",
                implementedField.Name,
                type.Kind.ToString().ToLowerInvariant(),
                type.Print())
            .SetType(type)
            .SetImplementedField(implementedField)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError InvalidArgumentType(
        IOutputField field,
        IOutputField implementedField,
        IInputField argument,
        IInputField implementedArgument)
        => SchemaErrorBuilder.New()
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
            .SetSpecifiedBy(field.DeclaringType.Kind)
            .Build();

    public static ISchemaError AdditionalArgumentNotNullable(
        IOutputField field,
        IOutputField implementedField,
        IInputField argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "The field `{0}` must only declare additional arguments to an " +
                "implemented field that are nullable.",
                field.Name)
            .SetType(field.DeclaringType)
            .SetField(field)
            .SetImplementedField(implementedField)
            .SetArgument(argument)
            .SetSpecifiedBy(field.DeclaringType.Kind)
            .Build();

    public static ISchemaError ArgumentNotImplemented(
        IOutputField field,
        IOutputField implementedField,
        IInputField missingArgument)
        => SchemaErrorBuilder.New()
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
            .SetSpecifiedBy(field.DeclaringType.Kind)
            .Build();

    public static ISchemaError OneofInputObjectMustHaveNullableFieldsWithoutDefaults(
        InputObjectType type,
        string[] fieldNames)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "Oneof Input Object `{0}` must only have nullable fields without default values. " +
                "Edit your type and make the field{1} `{2}` nullable and remove any defaults.",
                type.Name,
                fieldNames.Length is 1 ? string.Empty : "s",
                string.Join(", ", fieldNames))
            .SetType(type)
            .SetSpecifiedBy(type.Kind, rfc: 825)
            .Build();

    public static ISchemaError RequiredArgumentCannotBeDeprecated(
        IComplexOutputType type,
        IOutputField field,
        IInputField argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "Required argument {0} cannot be deprecated.",
                argument.Coordinate.ToString())
            .SetType(type)
            .SetField(field)
            .SetArgument(argument)
            .SetSpecifiedBy(type.Kind, rfc: 805)
            .Build();

    public static ISchemaError RequiredArgumentCannotBeDeprecated(
        DirectiveType directive,
        IInputField argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "Required argument {0} cannot be deprecated.",
                argument.Coordinate.ToString())
            .SetDirective(directive)
            .SetArgument(argument)
            .SetSpecifiedBy(TypeKind.Directive, rfc: 805)
            .Build();

    public static ISchemaError RequiredFieldCannotBeDeprecated(
        IInputObjectType type,
        IInputField field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                "Required input field {0} cannot be deprecated.",
                field.Coordinate.ToString())
            .SetType(type)
            .SetField(field)
            .SetSpecifiedBy(TypeKind.InputObject, rfc: 805)
            .Build();

    private static ISchemaErrorBuilder SetType(
        this ISchemaErrorBuilder errorBuilder,
        INamedType type)
        => errorBuilder
            .AddSyntaxNode(type.SyntaxNode)
            .SetTypeSystemObject((TypeSystemObjectBase)type);

    private static ISchemaErrorBuilder SetDirective(
        this ISchemaErrorBuilder errorBuilder,
        DirectiveType type)
        => errorBuilder
            .AddSyntaxNode(type.SyntaxNode)
            .SetTypeSystemObject(type);

    private static ISchemaErrorBuilder SetField(
        this ISchemaErrorBuilder errorBuilder,
        IField field,
        string name = "field")
        => errorBuilder
            .AddSyntaxNode(field.SyntaxNode)
            .SetExtension(name, field);

    private static ISchemaErrorBuilder SetArgument(
        this ISchemaErrorBuilder errorBuilder,
        IInputField field)
        => errorBuilder.SetField(field, "argument");

    private static ISchemaErrorBuilder SetImplementedType(
        this ISchemaErrorBuilder errorBuilder,
        IComplexOutputType type)
        => errorBuilder
            .AddSyntaxNode(type.SyntaxNode)
            .SetExtension("implementedType", type);

    private static ISchemaErrorBuilder SetImplementedField(
        this ISchemaErrorBuilder errorBuilder,
        IOutputField field)
        => errorBuilder.SetField(field, "implementedField");

    private static ISchemaErrorBuilder SetImplementedArgument(
        this ISchemaErrorBuilder errorBuilder,
        IInputField field)
        => errorBuilder.SetField(field, "implementedArgument");

    private static ISchemaErrorBuilder SetSpecifiedBy(
        this ISchemaErrorBuilder errorBuilder,
        TypeKind kind,
        int? rfc = null)
    {
        errorBuilder
            .SpecifiedBy(_interfaceTypeValidation, kind is TypeKind.Interface)
            .SpecifiedBy(_objectTypeValidation, kind is TypeKind.Object)
            .SpecifiedBy(_inputObjectTypeValidation, kind is TypeKind.InputObject)
            .SpecifiedBy(_directiveValidation, kind is TypeKind.Directive);

        if (rfc.HasValue)
        {
            errorBuilder.SetExtension(
                "rfc",
                "https://github.com/graphql/graphql-spec/pull/" + rfc.Value);
        }

        return errorBuilder;
    }

    public static ISchemaError InterfaceHasNoImplementation(
        InterfaceType interfaceType)
        => SchemaErrorBuilder.New()
            .SetMessage(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "There is no object type implementing interface `{0}`.",
                    interfaceType.Name))
            .SetCode(ErrorCodes.Schema.InterfaceNotImplemented)
            .SetTypeSystemObject(interfaceType)
            .AddSyntaxNode(interfaceType.SyntaxNode)
            .Build();
}
