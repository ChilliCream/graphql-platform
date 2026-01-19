using System.Globalization;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Utilities;

internal static class ErrorHelper
{
    private const string InterfaceTypeValidation = "sec-Interfaces.Type-Validation";
    private const string ObjectTypeValidation = "sec-Objects.Type-Validation";
    private const string InputObjectTypeValidation = "sec-Input-Objects.Type-Validation";
    private const string DirectiveValidation = "sec-Type-System.Directives.Validation";

    public static ISchemaError NeedsOneAtLeastField(ITypeDefinition type)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_NeedsOneAtLeastField,
                type.Kind.ToString().ToLowerInvariant(),
                type.Name)
            .SetType(type)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedField(
        ITypeDefinition type,
        IFieldDefinition field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_TwoUnderscoresNotAllowedField)
            .SetType(type)
            .SetField(field)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedOnArgument(
        IComplexTypeDefinition type,
        IOutputFieldDefinition field,
        IInputValueDefinition argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_TwoUnderscoresNotAllowedOnArgument)
            .SetType(type)
            .SetField(field)
            .SetArgument(argument)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedOnArgument(
        IDirectiveDefinition directiveDefinition,
        IInputValueDefinition argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_TwoUnderscoresNotAllowedOnArgument)
            .SetDirective(directiveDefinition)
            .SetArgument(argument)
            .SetSpecifiedBy(TypeKind.Directive)
            .Build();

    public static ISchemaError TwoUnderscoresNotAllowedOnDirectiveName(IDirectiveDefinition directiveDefinition)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_TwoUnderscoresNotAllowedOnDirectiveName)
            .SetDirective(directiveDefinition)
            .SetSpecifiedBy(TypeKind.Directive)
            .Build();

    public static ISchemaError NotTransitivelyImplemented(
        IComplexTypeDefinition type,
        IComplexTypeDefinition implementedType)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_NotTransitivelyImplemented,
                type.Kind.ToString().ToLowerInvariant())
            .SetType(type)
            .SetImplementedType(implementedType)
            .SetSpecifiedBy(type.Kind)
            .Build();

    public static ISchemaError InvalidFieldType(
        IComplexTypeDefinition type,
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField)
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

    public static ISchemaError InvalidFieldDeprecation(
        string implementedTypeName,
        IOutputFieldDefinition implementedField,
        IComplexTypeDefinition type,
        IOutputFieldDefinition field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_InvalidFieldDeprecation,
                implementedTypeName,
                implementedField.Name,
                type.Name,
                field.Name)
            .SetType(type)
            .SetField(field)
            .SetImplementedField(implementedField)
            .SetSpecifiedBy(type.Kind, isDraft: true)
            .Build();

    public static ISchemaError FieldNotImplemented(
        IComplexTypeDefinition type,
        IOutputFieldDefinition implementedField)
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
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField,
        IInputValueDefinition argument,
        IInputValueDefinition implementedArgument)
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
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField,
        IInputValueDefinition argument)
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
        IOutputFieldDefinition field,
        IOutputFieldDefinition implementedField,
        IInputValueDefinition missingArgument)
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
                ErrorHelper_OneOfInputObjectMustHaveNullableFieldsWithoutDefaults,
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
        IComplexTypeDefinition type,
        IOutputFieldDefinition field,
        IInputValueDefinition argument)
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
        IDirectiveDefinition directive,
        IInputValueDefinition argument)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_RequiredArgumentCannotBeDeprecated,
                argument.Coordinate.ToString())
            .SetDirective(directive)
            .SetArgument(argument)
            .SetSpecifiedBy(TypeKind.Directive, rfc: 805)
            .Build();

    public static ISchemaError RequiredFieldCannotBeDeprecated(
        IInputObjectTypeDefinition type,
        IInputValueDefinition field)
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
        ITypeDefinition type)
        => errorBuilder.SetTypeSystemObject((TypeSystemObject)type);

    private static SchemaErrorBuilder SetDirective(
        this SchemaErrorBuilder errorBuilder,
        IDirectiveDefinition type)
        => errorBuilder.SetTypeSystemObject((TypeSystemObject)type);

    private static SchemaErrorBuilder SetField(
        this SchemaErrorBuilder errorBuilder,
        IFieldDefinition field,
        string name = "field")
        => errorBuilder.SetExtension(name, field);

    private static SchemaErrorBuilder SetArgument(
        this SchemaErrorBuilder errorBuilder,
        IInputValueDefinition field)
        => errorBuilder.SetField(field, "argument");

    private static SchemaErrorBuilder SetImplementedType(
        this SchemaErrorBuilder errorBuilder,
        ITypeDefinition type)
        => errorBuilder.SetExtension("implementedType", type);

    private static SchemaErrorBuilder SetImplementedField(
        this SchemaErrorBuilder errorBuilder,
        IOutputFieldDefinition field)
        => errorBuilder.SetField(field, "implementedField");

    private static SchemaErrorBuilder SetImplementedArgument(
        this SchemaErrorBuilder errorBuilder,
        IInputValueDefinition field)
        => errorBuilder.SetField(field, "implementedArgument");

    private static SchemaErrorBuilder SetSpecifiedBy(
        this SchemaErrorBuilder errorBuilder,
        TypeKind kind,
        bool isDraft = false,
        int? rfc = null)
    {
        errorBuilder
            .SpecifiedBy(InterfaceTypeValidation, kind is TypeKind.Interface, isDraft)
            .SpecifiedBy(ObjectTypeValidation, kind is TypeKind.Object, isDraft)
            .SpecifiedBy(InputObjectTypeValidation, kind is TypeKind.InputObject, isDraft)
            .SpecifiedBy(DirectiveValidation, kind is TypeKind.Directive, isDraft);

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
        TypeSystemObject interfaceOrObject)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_CompleteInterfacesHelper_UnableToResolveInterface)
            .SetCode(ErrorCodes.Schema.MissingType)
            .SetTypeSystemObject(interfaceOrObject)
            .Build();

    public static ISchemaError DirectiveCollection_DirectiveIsUnique(
        DirectiveType directiveType,
        TypeSystemObject type,
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
        TypeSystemObject type,
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
        LeafCoercionException exception)
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
        ObjectFieldConfiguration field)
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
        TypeSystemObject type)
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
        TypeSystemObject type,
        string currentOrder)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_MiddlewareOrderInvalid, fieldCoordinate, currentOrder)
            .SetCode(ErrorCodes.Schema.MiddlewareOrderInvalid)
            .SetTypeSystemObject(type)
            .SetExtension(nameof(fieldCoordinate), fieldCoordinate)
            .Build();

    public static ISchemaError DuplicateDataMiddlewareDetected(
        SchemaCoordinate field,
        TypeSystemObject type,
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
        TypeSystemObject type,
        Type runtimeType)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_NoSchemaTypesAllowedAsRuntimeType,
                type.Name,
                runtimeType.FullName ?? runtimeType.Name)
            .SetCode(ErrorCodes.Schema.NoSchemaTypesAllowedAsRuntimeType)
            .SetTypeSystemObject(type)
            .Build();

    public static IError Relay_NoNodeResolver(string typeName, Path path, Selection selection)
        => ErrorBuilder.New()
            .SetMessage(ErrorHelper_Relay_NoNodeResolver, typeName)
            .SetPath(path)
            .Build();

    public static ISchemaError NodeResolver_MustHaveExactlyOneIdArg(
        string fieldName,
        TypeSystemObject type)
        => SchemaErrorBuilder
            .New()
            .SetMessage(ErrorHelper_NodeResolver_MustHaveExactlyOneIdArg, fieldName)
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError NodeResolver_MustReturnObject(
        string fieldName,
        TypeSystemObject type)
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
        Selection selection,
        Path path,
        int maxAllowedNodes,
        int requestNodes)
        => ErrorBuilder.New()
            .SetMessage(
                ErrorHelper_FetchedToManyNodesAtOnce,
                maxAllowedNodes,
                requestNodes)
            .SetPath(path)
            .SetCode(ErrorCodes.Execution.FetchedToManyNodesAtOnce)
            .Build();

    public static ISchemaError NoFields(
        TypeSystemObject typeSystemObj,
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
        TypeSystemObject type,
        ITypeSystemMember declaringMember,
        IReadOnlyCollection<string> duplicateFieldNames)
    {
        var field = declaringMember is IType
            ? "field"
            : "argument";

        var coordinate = declaringMember is IType
            ? new SchemaCoordinate(type.Name)
            : new SchemaCoordinate(type.Name, ((INameProvider)declaringMember).Name);

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
