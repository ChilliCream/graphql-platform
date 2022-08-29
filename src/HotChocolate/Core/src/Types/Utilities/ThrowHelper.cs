using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Utilities;

internal static class ThrowHelper
{
    public static ArgumentException String_NullOrEmpty(
        string parameterName) =>
        new ArgumentException(
            $@"'{parameterName}' cannot be null or empty",
            parameterName);

    public static GraphQLException EventMessage_InvalidCast(
        Type expectedType,
        Type messageType) =>
        new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    "The event message is of the type `{0}` and cannot be casted to `{1}.`",
                    messageType.FullName!,
                    expectedType.FullName!)
                .Build());

    public static GraphQLException EventMessage_NotFound() =>
        new GraphQLException(
            ErrorBuilder.New()
                .SetMessage("There is no event message on the context.")
                .Build());

    public static SchemaException SubscribeAttribute_MessageTypeUnspecified(
        MemberInfo member) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "You need to specify the message type on {0}.{1}. (SubscribeAttribute)",
                    member.DeclaringType!.FullName,
                    member.Name)
                .SetExtension("member", member)
                .Build());

    public static SchemaException SubscribeAttribute_TopicTypeUnspecified(
        MemberInfo member) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "You need to specify the topic type on {0}.{1}. (SubscribeAttribute)",
                    member.DeclaringType!.FullName,
                    member.Name)
                .SetExtension("member", member)
                .Build());

    public static SchemaException SubscribeAttribute_TopicOnParameterAndMethod(
        MemberInfo member) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The topic is declared multiple times on {0}.{1}. (TopicAttribute)",
                    member.DeclaringType!.FullName,
                    member.Name)
                .SetExtension("member", member)
                .Build());

    public static SchemaException SubscribeAttribute_SubscribeResolverNotFound(
        MemberInfo member,
        string subscribeResolverName) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to find the subscribe resolver `{2}` defined on {0}.{1}. " +
                    "The subscribe resolver bust be a method that is public, non-static " +
                    "and on the same type as the resolver. (SubscribeAttribute)",
                    member.DeclaringType!.FullName,
                    member.Name,
                    subscribeResolverName)
                .SetExtension("member", member)
                .Build());

    public static SchemaException Convention_UnableToCreateConvention(
        Type convention) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to create a convention instance from {0}.",
                    convention.FullName ?? convention.Name)
                .Build());

    public static SchemaException UsePagingAttribute_NodeTypeUnknown(
        MemberInfo member) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_UsePagingAttribute_NodeTypeUnknown)
                .SetCode(ErrorCodes.Paging.NodeTypeUnknown)
                .SetExtension(nameof(member), member)
                .Build());

    public static SchemaException TypeRegistrar_CreateInstanceFailed(
        Type namedSchemaType,
        Exception exception) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to create instance of type `{0}`.",
                    namedSchemaType.FullName)
                .SetException(exception)
                .SetExtension(nameof(namedSchemaType), namedSchemaType)
                .Build());

    public static SchemaException TypeCompletionContext_UnableToResolveType(
        ITypeSystemObject type,
        ITypeReference typeRef) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to resolve type reference `{0}`.",
                    typeRef)
                .SetTypeSystemObject(type)
                .SetExtension(nameof(typeRef), typeRef)
                .Build());

    public static SchemaException TypeInitializer_DuplicateTypeName(
        ITypeSystemObject type,
        ITypeSystemObject otherType) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    TypeResources.TypeInitializer_CompleteName_Duplicate,
                    type.Name)
                .SetTypeSystemObject(type)
                .SetExtension(nameof(otherType), otherType)
                .Build());

    public static SchemaException NodeAttribute_NodeResolverNotFound(
        Type type,
        string nodeResolver) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The specified node resolver `{0}` does not exist on `{1}`.",
                    nodeResolver,
                    type.FullName ?? type.Name)
                .Build());

    public static SchemaException NodeAttribute_IdFieldNotFound(
        Type type,
        string idField) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The specified id field `{0}` does not exist on `{1}`.",
                    idField,
                    type.FullName ?? type.Name)
                .Build());

#nullable enable
    public static SchemaException Convention_TwoConventionsRegisteredForScope(
        Type conventionType,
        IConvention first,
        IConvention other,
        string? scope) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "There are two conventions registered for {0} in scope {1}. Only one " +
                    "convention is allowed. Use convention extensions if additional " +
                    "configuration is needed. Colliding conventions are {2} and {3}",
                    conventionType.FullName ?? conventionType.Name,
                    scope ?? "default",
                    first.GetType().FullName ?? first.GetType().Name,
                    other.GetType().FullName ?? other.GetType().Name)
                .Build());

    public static SchemaException Convention_ConventionCouldNotBeCreated(
        Type conventionType,
        string? scope) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Convention of type {0} in scope {1} could not be created",
                    conventionType.FullName ?? conventionType.Name,
                    scope ?? "default")
                .Build());

    public static SchemaException DataLoader_InvalidType(
        Type dataLoaderType) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The provided type {0} is not a dataloader",
                    dataLoaderType.FullName ?? dataLoaderType.Name)
                .Build());

    public static SchemaException NonGenericExecutableNotAllowed() =>
        new SchemaException(
            SchemaErrorBuilder
                .New()
                .SetMessage(ExtendedTypeReferenceHandler_NonGenericExecutableNotAllowed)
                .Build());

    public static SerializationException RequiredInputFieldIsMissing(
        InputField field,
        Path fieldPath)
        => new SerializationException(
            ErrorBuilder.New()
                .SetMessage(
                    "The required input field `{0}` is missing.",
                    field.Name)
                .SetPath(fieldPath)
                .SetExtension("field", field.Coordinate.ToString())
                .Build(),
            field.Type,
            fieldPath);

    public static SerializationException InvalidInputFieldNames(
        InputObjectType type,
        IReadOnlyList<string> invalidFieldNames,
        Path path)
    {
        if (invalidFieldNames.Count == 1)
        {
            throw new SerializationException(
                ErrorBuilder.New()
                    .SetMessage(
                        "The field `{0}` does not exist on the type `{1}`.",
                        invalidFieldNames[0],
                        type.Name)
                    .SetPath(path)
                    .SetExtension("type", type.Name)
                    .Build(),
                type,
                path);
        }

        throw new SerializationException(
            ErrorBuilder.New()
                .SetMessage(
                    "The fields `{0}` do not exist on the type `{1}`.",
                    string.Join(", ", invalidFieldNames.Select(t => $"`{t}`")),
                    type.Name)
                .SetPath(path)
                .SetExtension("type", type.Name)
                .Build(),
            type,
            path);
    }

    public static SerializationException OneOfNoFieldSet(
        InputObjectType type,
        Path? path)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ThrowHelper_OneOfNoFieldSet, type.Name)
            .SetCode(ErrorCodes.Execution.OneOfNoFieldSet)
            .SetPath(path);

        return new(builder.Build(), type, path);
    }

    public static SerializationException OneOfMoreThanOneFieldSet(
        InputObjectType type,
        Path? path)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ThrowHelper_OneOfMoreThanOneFieldSet, type.Name)
            .SetCode(ErrorCodes.Execution.OneOfMoreThanOneFieldSet)
            .SetPath(path);

        return new(builder.Build(), type, path);
    }

    public static SerializationException OneOfFieldIsNull(
        InputObjectType type,
        Path? path,
        InputField field)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ThrowHelper_OneOfFieldIsNull, field.Name, type.Name)
            .SetCode(ErrorCodes.Execution.OneOfFieldIsNull)
            .SetPath(path)
            .SetExtension(nameof(field), field.Coordinate.ToString());

        return new(builder.Build(), type, path);
    }

    public static SerializationException NonNullInputViolation(
        IType type,
        Path? path,
        InputField? field = null)
    {
        var builder = ErrorBuilder.New()
            .SetMessage("Cannot accept null for non-nullable input.")
            .SetCode(ErrorCodes.Execution.NonNullViolation)
            .SetPath(path);

        if (field is not null)
        {
            builder.SetExtension(nameof(field), field.Coordinate.ToString());
        }

        return new(builder.Build(), type, path);
    }

    public static SerializationException ParseInputObject_InvalidSyntaxKind(
        InputObjectType type,
        SyntaxKind kind,
        Path path)
        => new SerializationException(
            ErrorBuilder.New()
            .SetMessage(
                "The syntax node `{0}` is incompatible with the type `{1}`.",
                kind,
                type.Name)
            .SetPath(path)
            .SetExtension(nameof(type), type.Name)
            .Build(),
            type,
            path);

    public static SerializationException ParseInputObject_InvalidObjectKind(
        InputObjectType type,
        Type objectType,
        Path path)
        => new SerializationException(
            ErrorBuilder.New()
            .SetMessage(
                "The input object `{1}` must to be serialized as `{2}` or as " +
                "`IReadOnlyDictionary<string. object?>` but not as `{0}`.",
                objectType.FullName ?? objectType.Name,
                type.Name,
                type.RuntimeType.FullName ?? type.RuntimeType.Name)
            .SetPath(path)
            .SetExtension(nameof(type), type.Name)
            .Build(),
            type,
            path);


    public static SerializationException ParseNestedList_InvalidSyntaxKind(
        ListType type,
        SyntaxKind kind,
        Path path)
        => new SerializationException(
            ErrorBuilder.New()
            .SetMessage(
                "The item syntax node for a nested list must be " +
                "`ListValue` but the parser found `{0}`.",
                kind)
            .SetPath(path)
            .SetExtension(
                "specifiedBy",
                "https://spec.graphql.org/June2018/#sec-Type-System.List")
            .Build(),
            type,
            path);

    public static SerializationException ParseList_InvalidObjectKind(
        ListType type,
        Type listType,
        Path path)
        => new SerializationException(
            ErrorBuilder.New()
            .SetMessage(
                "The list `{1}` must to be serialized as `{2}` or as " +
                "`IList` but not as `{0}`.",
                listType.FullName ?? listType.Name,
                type.Print(),
                type.RuntimeType.FullName ?? type.RuntimeType.Name)
            .Build(),
            type,
            path);

    public static SerializationException FormatValueList_InvalidObjectKind(
        ListType type,
        Type listType,
        Path path)
        => new SerializationException(
            ErrorBuilder.New()
                .SetMessage(
                    "The list runtime value of {0} must implement IEnumerable or IList " +
                    "but is of the type {1}.",
                    type.Print(),
                    listType.FullName ?? listType.Name)
                .SetPath(path)
                .Build(),
            type,
            path);

    public static SerializationException FormatResultObject_InvalidObjectKind(
        InputObjectType type,
        Type objectType,
        Path path)
        => new SerializationException(
            ErrorBuilder.New()
                .SetMessage(
                    "The input object `{1}` must to be of type `{2}` or serialized as " +
                    "`IReadOnlyDictionary<string. object?>` but not as `{0}`.",
                    objectType.FullName ?? objectType.Name,
                    type.Name,
                    type.RuntimeType.FullName ?? type.RuntimeType.Name)
                .SetPath(path)
                .SetExtension(nameof(type), type.Name)
                .Build(),
            type,
            path);

    public static SerializationException FormatResultList_InvalidObjectKind(
        ListType type,
        Type listType,
        Path path)
        => new SerializationException(
            ErrorBuilder.New()
                .SetMessage(
                    "The list result value of {0} must implement IList " +
                    "but is of the type {1}.",
                    type.Print(),
                    listType.FullName ?? listType.Name)
                .SetPath(path)
                .Build(),
            type,
            path);

    public static SerializationException FormatResultLeaf_InvalidSyntaxKind(
        IType type,
        SyntaxKind kind,
        Path path)
        => new SerializationException(
            ErrorBuilder.New()
                .SetMessage(
                    "The type `{0}` does mot expect `{1}`.",
                    type.Print(),
                    kind)
                .SetPath(path)
                .Build(),
            type,
            path);

    public static InvalidOperationException RewriteNullability_InvalidNullabilityStructure()
        => new(AbstractionResources.ThrowHelper_TryRewriteNullability_InvalidNullabilityStructure);

    public static InvalidSchemaCoordinateException Schema_GetMember_DirectiveArgumentNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                "Argument `{0}` was not found on directive `@{1}`.",
                coordinate.ArgumentName!,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_DirectiveNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                "Directive `@{0}` not found.",
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_EnumValueNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                "Enum value `{0}` was not found on type `{1}`.",
                coordinate.MemberName!,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_InputFieldNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                "Input field `{0}` was not found on type `{1}`.",
                coordinate.MemberName!,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_InvalidCoordinate(
        SchemaCoordinate coordinate,
        INamedType type)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                "The coordinate `{0}` is invalid for the type `{1}`.",
                coordinate.ToString(),
                type.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_FieldArgNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                "Argument `{0}` was not found on field `{1}.{2}`.",
                coordinate.ArgumentName!,
                coordinate.Name,
                coordinate.MemberName!),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_FieldNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                "Field `{0}` was not found on type `{1}`.",
                coordinate.MemberName!,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_TypeNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                "A type with the name `{0}` was not found.",
                coordinate.Name),
            coordinate);

    public static InvalidOperationException FieldBase_Sealed()
        => new(ThrowHelper_FieldBase_Sealed);

    public static InvalidOperationException NodeResolver_ArgumentTypeMissing()
        => new("A field argument at this initialization state is guaranteed to have an argument type, but we found none.");

    public static InvalidOperationException NodeResolver_ObjNoDefinition()
        => new("An object type at this point is guaranteed to have a type definition, but we found none.");
}
