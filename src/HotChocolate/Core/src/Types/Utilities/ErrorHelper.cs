using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;
using IHasName = HotChocolate.Types.IHasName;

#nullable enable

namespace HotChocolate.Utilities;

internal static class ErrorHelper
{
    private const string _interfaceTypeValidation = "sec-Interfaces.Type-Validation";
    private const string _objectTypeValidation = "sec-Objects.Type-Validation";
    private const string _inputObjectTypeValidation = "sec-Input-Objects.Type-Validation";
    private const string _directiveValidation = "sec-Type-System.Directives.Validation";

    public static ISchemaError NeedsOneAtLeastField(INamedType type)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_NeedsOneAtLeastField,
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
                ErrorHelper_TwoUnderscoresNotAllowedField)
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
                ErrorHelper_TwoUnderscoresNotAllowedOnArgument)
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
                ErrorHelper_TwoUnderscoresNotAllowedOnArgument)
            .SetDirective(type)
            .SetArgument(argument)
            .SetSpecifiedBy(TypeKind.Directive)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedOnDirectiveName(DirectiveType type)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_TwoUnderscoresNotAllowedOnDirectiveName)
            .SetDirective(type)
            .SetSpecifiedBy(TypeKind.Directive)
            .Build();

    public static ISchemaError NotTransitivelyImplemented(
        IComplexOutputType type,
        IComplexOutputType implementedType)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_NotTransitivelyImplemented,
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
                ErrorHelper_InvalidFieldType,
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
                ErrorHelper_FieldNotImplemented,
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
                ErrorHelper_InvalidArgumentType,
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
                ErrorHelper_AdditionalArgumentNotNullable,
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
                ErrorHelper_ArgumentNotImplemented,
                missingArgument.Name,
                field.Name,
                field.Name,
                implementedField.DeclaringType.Print())
            .SetType(field.DeclaringType)
            .SetField(field)
            .SetImplementedField(implementedField)
            .SetExtension("missingArgument", missingArgument)
            .SetSpecifiedBy(field.DeclaringType.Kind)
            .Build();

    public static ISchemaError OneOfInputObjectMustHaveNullableFieldsWithoutDefaults(
        InputObjectType type,
        string[] fieldNames)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_OneofInputObjectMustHaveNullableFieldsWithoutDefaults,
                type.Name,
                fieldNames.Length is 1 ? string.Empty : "s",
                string.Join(", ", fieldNames))
            .SetType(type)
            .SetSpecifiedBy(type.Kind, rfc: 825)
            .Build();

    public static ISchemaError InputObjectMustNotHaveRecursiveNonNullableReferencesToSelf(
        InputObjectType type,
        IEnumerable<string> path)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_InputObjectMustNotHaveRecursiveNonNullableReferencesToSelf,
                type.Name,
                string.Join(" --> ", path))
            .SetType(type)
            .SetSpecifiedBy(type.Kind, rfc: 445)
            .Build();

    public static ISchemaError RequiredArgumentCannotBeDeprecated(
        IComplexOutputType type,
        IOutputField field,
        IInputField argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_RequiredArgumentCannotBeDeprecated,
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
                ErrorHelper_RequiredArgumentCannotBeDeprecated,
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
                ErrorHelper_RequiredFieldCannotBeDeprecated,
                field.Coordinate.ToString())
            .SetType(type)
            .SetField(field)
            .SetSpecifiedBy(TypeKind.InputObject, rfc: 805)
            .Build();

    public static ISchemaError DirectiveType_NoLocations(string name, DirectiveType type)
        => SchemaErrorBuilder.New()
            .SetMessage(
                string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.DirectiveType_NoLocations,
                    name))
            .SetCode(ErrorCodes.Schema.MissingType)
            .SetTypeSystemObject(type)
            .Build();

    private static SchemaErrorBuilder SetType(
        this SchemaErrorBuilder errorBuilder,
        INamedType type)
        => errorBuilder.SetTypeSystemObject((TypeSystemObjectBase)type);

    private static SchemaErrorBuilder SetDirective(
        this SchemaErrorBuilder errorBuilder,
        DirectiveType type)
        => errorBuilder.SetTypeSystemObject(type);

    private static SchemaErrorBuilder SetField(
        this SchemaErrorBuilder errorBuilder,
        IField field,
        string name = "field")
        => errorBuilder.SetExtension(name, field);

    private static SchemaErrorBuilder SetArgument(
        this SchemaErrorBuilder errorBuilder,
        IInputField field)
        => errorBuilder.SetField(field, "argument");

    private static SchemaErrorBuilder SetImplementedType(
        this SchemaErrorBuilder errorBuilder,
        IComplexOutputType type)
        => errorBuilder.SetExtension("implementedType", type);

    private static SchemaErrorBuilder SetImplementedField(
        this SchemaErrorBuilder errorBuilder,
        IOutputField field)
        => errorBuilder.SetField(field, "implementedField");

    private static SchemaErrorBuilder SetImplementedArgument(
        this SchemaErrorBuilder errorBuilder,
        IInputField field)
        => errorBuilder.SetField(field, "implementedArgument");

    private static SchemaErrorBuilder SetSpecifiedBy(
        this SchemaErrorBuilder errorBuilder,
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
                    ErrorHelper_InterfaceHasNoImplementation,
                    interfaceType.Name))
            .SetCode(ErrorCodes.Schema.InterfaceNotImplemented)
            .SetTypeSystemObject(interfaceType)
            .Build();

    public static ISchemaError CompleteInterfacesHelper_UnableToResolveInterface(
        ITypeSystemObject interfaceOrObject)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_CompleteInterfacesHelper_UnableToResolveInterface)
            .SetCode(ErrorCodes.Schema.MissingType)
            .SetTypeSystemObject(interfaceOrObject)
            .Build();

    public static ISchemaError DirectiveCollection_DirectiveIsUnique(
        DirectiveType directiveType,
        ITypeSystemObject type,
        DirectiveNode? syntaxNode,
        object source)
        => SchemaErrorBuilder.New()
            .SetMessage(
                TypeResources.DirectiveCollection_DirectiveIsUnique,
                directiveType.Name)
            .SetCode(ErrorCodes.Schema.MissingType)
            .SetTypeSystemObject(type)
            .AddSyntaxNode(syntaxNode)
            .SetExtension("Source", source)
            .Build();

    public static ISchemaError DirectiveCollection_LocationNotAllowed(
        DirectiveType directiveType,
        Types.DirectiveLocation location,
        ITypeSystemObject type,
        DirectiveNode? syntaxNode,
        object source)
        => SchemaErrorBuilder.New()
            .SetMessage(
                TypeResources.DirectiveCollection_LocationNotAllowed,
                directiveType.Name,
                location)
            .SetCode(ErrorCodes.Schema.MissingType)
            .SetTypeSystemObject(type)
            .AddSyntaxNode(syntaxNode)
            .SetExtension("Source", source)
            .Build();

    public static ISchemaError DirectiveCollection_ArgumentError(
        DirectiveType directiveType,
        DirectiveNode? syntaxNode,
        object source,
        Path path,
        SerializationException exception)
    {
        var message = string.Format(
            ErrorHelper_DirectiveCollection_ArgumentValueTypeIsWrong,
            exception.Message,
            path);

        if (syntaxNode is not null)
        {
            message += Environment.NewLine;
            message += syntaxNode.ToString(true);
        }

        return SchemaErrorBuilder.New()
            .SetMessage(message)
            .SetCode(ErrorCodes.Schema.InvalidArgument)
            .SetTypeSystemObject(directiveType)
            .AddSyntaxNode(syntaxNode)
            .SetExtension("Source", source)
            .Build();
    }

    public static ISchemaError ObjectType_UnableToInferOrResolveType(
        string typeName,
        ObjectType type,
        ObjectFieldDefinition field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_ObjectType_UnableToInferOrResolveType,
                typeName,
                field.Name)
            .SetCode(ErrorCodes.Schema.NoFieldType)
            .SetTypeSystemObject(type)
            .SetPath(Path.FromList(typeName, field.Name))
            .SetExtension(TypeErrorFields.Definition, field)
            .Build();

    public static ISchemaError ObjectField_HasNoResolver(
        string typeName,
        string fieldName,
        ITypeSystemObject type)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_ObjectField_HasNoResolver,
                typeName,
                fieldName)
            .SetCode(ErrorCodes.Schema.NoResolver)
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError MiddlewareOrderInvalid(
        SchemaCoordinate fieldCoordinate,
        ITypeSystemObject type,
        string currentOrder)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_MiddlewareOrderInvalid, fieldCoordinate, currentOrder)
            .SetCode(ErrorCodes.Schema.MiddlewareOrderInvalid)
            .SetTypeSystemObject(type)
            .SetExtension(nameof(fieldCoordinate), fieldCoordinate)
            .Build();

    public static ISchemaError DuplicateDataMiddlewareDetected(
        SchemaCoordinate field,
        ITypeSystemObject type,
        IEnumerable<string> duplicateMiddleware)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_DuplicateDataMiddlewareDetected_Message,
                field.ToString(),
                string.Join(", ", duplicateMiddleware))
            .SetCode(ErrorCodes.Schema.MiddlewareOrderInvalid)
            .SetTypeSystemObject(type)
            .SetExtension(nameof(field), field)
            .Build();

    public static ISchemaError NoSchemaTypesAllowedAsRuntimeType(
        ITypeSystemObject type,
        Type runtimeType)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_NoSchemaTypesAllowedAsRuntimeType,
                type.Name,
                runtimeType.FullName ?? runtimeType.Name)
            .SetCode(ErrorCodes.Schema.NoSchemaTypesAllowedAsRuntimeType)
            .SetTypeSystemObject(type)
            .Build();

    public static IError Relay_NoNodeResolver(string typeName, Path path, IReadOnlyList<FieldNode> fieldNodes)
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_Relay_NoNodeResolver, typeName)
            .SetPath(path)
            .SetLocations(fieldNodes)
            .Build();

    public static ISchemaError NodeResolver_MustHaveExactlyOneIdArg(
        string fieldName,
        ITypeSystemObject type)
        => SchemaErrorBuilder
            .New()
            .SetMessage(ErrorHelper_NodeResolver_MustHaveExactlyOneIdArg, fieldName)
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError NodeResolver_MustReturnObject(
        string fieldName,
        ITypeSystemObject type)
        => SchemaErrorBuilder
            .New()
            .SetMessage(ErrorHelper_NodeResolver_MustReturnObject, fieldName)
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError NodeResolver_NodeTypeHasNoId(
        ObjectType type)
        => SchemaErrorBuilder
            .New()
            .SetMessage(ErrorHelper_NodeResolver_NodeTypeHasNoId, type.Name)
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError NodeResolverMissing(
        ObjectType type)
        => SchemaErrorBuilder.New()
            .SetMessage(string.Format(ErrorHelper_NodeResolverMissing, type.Name))
            .SetCode(ErrorCodes.Schema.NodeResolverMissing)
            .SetTypeSystemObject(type)
            .Build();

    public static IError FetchedToManyNodesAtOnce(
        IReadOnlyList<FieldNode> fieldNodes,
        Path path,
        int maxAllowedNodes,
        int requestNodes)
        => ErrorBuilder.New()
            .SetMessage(
                ErrorHelper_FetchedToManyNodesAtOnce,
                maxAllowedNodes,
                requestNodes)
            .SetLocations(fieldNodes)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.FetchedToManyNodesAtOnce)
            .Build();

    public static ISchemaError NoFields(
        ITypeSystemObject typeSystemObj,
        IType type)
        => SchemaErrorBuilder.New()
            .SetMessage(
                FieldInitHelper_NoFields,
                type.Kind.ToString(),
                typeSystemObj.Name)
            .SetCode(ErrorCodes.Schema.MissingType)
            .SetTypeSystemObject(typeSystemObj)
            .Build();

    public static ISchemaError DuplicateFieldName(
        ITypeSystemObject type,
        ITypeSystemMember declaringMember,
        IReadOnlyCollection<string> duplicateFieldNames)
    {
        var field = declaringMember is IType
            ? "field"
            : "argument";

        var coordinate = declaringMember is IType
            ? new SchemaCoordinate(type.Name)
            : new SchemaCoordinate(type.Name, ((IHasName)declaringMember).Name);

        var s = string.Empty;
        var @is = "is";

        if (duplicateFieldNames.Count > 1)
        {
            s = "s";
            @is = "are";
        }

        return SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_DuplicateFieldName_Message,
                field,
                s,
                string.Join(", ", duplicateFieldNames),
                @is,
                coordinate.ToString())
            .SetCode(ErrorCodes.Schema.DuplicateFieldNames)
            .SetTypeSystemObject(type)
            .Build();
    }
}
