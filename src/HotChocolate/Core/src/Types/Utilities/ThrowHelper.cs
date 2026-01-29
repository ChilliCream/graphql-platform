#nullable disable

using System.Globalization;
using System.Reflection;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Utilities;

internal static class ThrowHelper
{
    public static GraphQLException EventMessage_InvalidCast(
        Type expectedType,
        Type messageType)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_EventMessage_InvalidCast,
                    messageType.FullName,
                    expectedType.FullName)
                .Build());

    public static GraphQLException EventMessage_NotFound()
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_EventMessage_NotFound)
                .Build());

    public static SchemaException SubscribeAttribute_MessageTypeUnspecified(MemberInfo member)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_SubscribeAttribute_MessageTypeUnspecified,
                    member.DeclaringType!.FullName,
                    member.Name)
                .SetExtension("member", member)
                .Build());

    public static SchemaException SubscribeAttribute_TopicTypeUnspecified(MemberInfo member)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_SubscribeAttribute_TopicTypeUnspecified,
                    member.DeclaringType!.FullName,
                    member.Name)
                .SetExtension("member", member)
                .Build());

    public static SchemaException SubscribeAttribute_SubscribeResolverNotFound(
        MemberInfo member,
        string subscribeResolverName)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_SubscribeAttribute_SubscribeResolverNotFound,
                    member.DeclaringType!.FullName,
                    member.Name,
                    subscribeResolverName)
                .SetExtension("member", member)
                .Build());

    public static SchemaException Convention_UnableToCreateConvention(Type convention)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_Convention_UnableToCreateConvention,
                    convention.FullName ?? convention.Name)
                .Build());

    public static SchemaException UsePagingAttribute_NodeTypeUnknown(MemberInfo member)
        => new SchemaException(
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
                    ThrowHelper_TypeRegistrar_CreateInstanceFailed,
                    namedSchemaType.FullName)
                .SetException(exception)
                .SetExtension(nameof(namedSchemaType), namedSchemaType)
                .Build());

    public static SchemaException TypeCompletionContext_UnableToResolveType(
        TypeSystemObject type,
        TypeReference typeRef) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_TypeCompletionContext_UnableToResolveType,
                    typeRef)
                .SetTypeSystemObject(type)
                .SetExtension(nameof(typeRef), typeRef)
                .Build());

    public static SchemaException TypeInitializer_DuplicateTypeName(
        TypeSystemObject type,
        TypeSystemObject otherType) =>
        new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    TypeInitializer_CompleteName_Duplicate,
                    type.Name)
                .SetTypeSystemObject(type)
                .SetCode(ErrorCodes.Schema.DuplicateTypeName)
                .SetExtension(nameof(otherType), otherType)
                .Build());

    public static SchemaException TypeInitializer_MutationDuplicateErrorName(
        TypeSystemObject type,
        string mutationName,
        string errorName,
        IReadOnlyList<ISchemaError> originalErrors)
    {
        var mutationError = SchemaErrorBuilder.New()
            .SetMessage(
                ThrowHelper_MutationDuplicateErrorName,
                mutationName,
                errorName)
            .SetTypeSystemObject(type)
            .SetCode(ErrorCodes.Schema.DuplicateMutationErrorTypeName)
            .Build();

        var errors = new List<ISchemaError>(originalErrors);
        errors.Insert(0, mutationError);

        return new SchemaException(errors);
    }

    public static SchemaException NodeAttribute_IdFieldNotFound(
        Type type,
        string idField)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_NodeAttribute_IdFieldNotFound,
                    idField,
                    type.FullName ?? type.Name)
                .Build());

#nullable enable
    public static SchemaException Convention_TwoConventionsRegisteredForScope(
        Type conventionType,
        IConvention first,
        IConvention other,
        string? scope)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_Convention_TwoConventionsRegisteredForScope,
                    conventionType.FullName ?? conventionType.Name,
                    scope ?? "default",
                    first.GetType().FullName ?? first.GetType().Name,
                    other.GetType().FullName ?? other.GetType().Name)
                .Build());

    public static SchemaException Convention_ConventionCouldNotBeCreated(
        Type conventionType,
        string? scope)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_Convention_ConventionCouldNotBeCreated,
                    conventionType.FullName ?? conventionType.Name,
                    scope ?? "default")
                .Build());

    public static SchemaException DataLoader_InvalidType(Type dataLoaderType)
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_DataLoader_InvalidType,
                    dataLoaderType.FullName ?? dataLoaderType.Name)
                .Build());

    public static SchemaException NonGenericExecutableNotAllowed()
        => new SchemaException(
            SchemaErrorBuilder
                .New()
                .SetMessage(ExtendedTypeReferenceHandler_NonGenericExecutableNotAllowed)
                .Build());

    public static LeafCoercionException RequiredInputFieldIsMissing(
        IInputValueInfo field,
        Path fieldPath)
        => new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_RequiredInputFieldIsMissing,
                    field.Name)
                .SetInputPath(fieldPath)
                .SetExtension("field", field.Coordinate.ToString())
                .Build(),
            field.Type,
            fieldPath);

    public static LeafCoercionException InvalidInputFieldNames<T>(
        T type,
        IReadOnlyList<string> invalidFieldNames,
        Path path)
        where T : ITypeSystemMember, INameProvider
    {
        if (invalidFieldNames.Count == 1)
        {
            throw new LeafCoercionException(
                ErrorBuilder.New()
                    .SetMessage(
                        ThrowHelper_InvalidInputFieldNames_Single,
                        invalidFieldNames[0],
                        type.Name)
                    .SetInputPath(path)
                    .SetExtension("type", type.Name)
                    .Build(),
                type,
                path);
        }

        throw new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_InvalidInputFieldNames,
                    string.Join(", ", invalidFieldNames.Select(t => $"`{t}`")),
                    type.Name)
                .SetInputPath(path)
                .SetExtension("type", type.Name)
                .Build(),
            type,
            path);
    }

    public static LeafCoercionException OneOfNoFieldSet(
        InputObjectType type,
        Path? path)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ThrowHelper_OneOfNoFieldSet, type.Name)
            .SetCode(ErrorCodes.Execution.OneOfNoFieldSet)
            .SetInputPath(path);

        return new(builder.Build(), type, path);
    }

    public static LeafCoercionException OneOfMoreThanOneFieldSet(
        InputObjectType type,
        Path? path)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ThrowHelper_OneOfMoreThanOneFieldSet, type.Name)
            .SetCode(ErrorCodes.Execution.OneOfMoreThanOneFieldSet)
            .SetInputPath(path);

        return new(builder.Build(), type, path);
    }

    public static LeafCoercionException OneOfFieldIsNull(
        InputObjectType type,
        Path? path,
        InputField field)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ThrowHelper_OneOfFieldIsNull, field.Name, type.Name)
            .SetCode(ErrorCodes.Execution.OneOfFieldIsNull)
            .SetInputPath(path)
            .SetCoordinate(field.Coordinate);

        return new(builder.Build(), type, path);
    }

    public static LeafCoercionException NonNullInputViolation(
        ITypeSystemMember type,
        Path? path,
        IInputValueInfo? field = null)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ThrowHelper_NonNullInputViolation)
            .SetCode(ErrorCodes.Execution.NonNullViolation)
            .SetInputPath(path);

        if (field is not null)
        {
            builder.SetCoordinate(field.Coordinate);
        }

        return new(builder.Build(), type, path);
    }

    public static LeafCoercionException ParseInputObject_InvalidSyntaxKind(
        InputObjectType type,
        SyntaxKind kind,
        Path path)
        => new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_ParseInputObject_InvalidSyntaxKind,
                    kind,
                    type.Name)
                .SetInputPath(path)
                .SetExtension(nameof(type), type.Name)
                .Build(),
            type,
            path);

    public static LeafCoercionException ParseInputObject_InvalidValueKind(
        InputObjectType type,
        Path path)
        => new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_ParseInputObject_InvalidValueKind)
                .SetInputPath(path)
                .SetExtension(nameof(type), type.Name)
                .Build(),
            type,
            path);

    public static LeafCoercionException ParseNestedList_InvalidSyntaxKind(
        ListType type,
        SyntaxKind kind,
        Path path)
        => new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_ParseNestedList_InvalidSyntaxKind,
                    kind)
                .SetInputPath(path)
                .SetExtension(
                    "specifiedBy",
                    "https://spec.graphql.org/June2018/#sec-Type-System.List")
                .Build(),
            type,
            path);

    public static LeafCoercionException ParseList_InvalidValueKind(
        ListType type,
        Path path)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(ThrowHelper_ParseList_InvalidValueKind)
                .Build(),
            type,
            path);
    }

    public static LeafCoercionException FormatValueList_InvalidObjectKind(
        ListType type,
        Type listType,
        Path path)
        => new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_FormatValueList_InvalidObjectKind,
                    type.Print(),
                    listType.FullName ?? listType.Name)
                .SetInputPath(path)
                .Build(),
            type,
            path);

    public static LeafCoercionException FormatResultObject_InvalidObjectKind(
        InputObjectType type,
        Type objectType,
        Path path)
        => new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_FormatResultObject_InvalidObjectKind,
                    objectType.FullName ?? objectType.Name,
                    type.Name,
                    type.RuntimeType.FullName ?? type.RuntimeType.Name)
                .SetInputPath(path)
                .SetExtension(nameof(type), type.Name)
                .Build(),
            type,
            path);

    public static LeafCoercionException FormatResultList_InvalidObjectKind(
        ListType type,
        Type listType,
        Path path)
        => new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_FormatResultList_InvalidObjectKind,
                    type.Print(),
                    listType.FullName ?? listType.Name)
                .SetInputPath(path)
                .Build(),
            type,
            path);

    public static LeafCoercionException FormatResultLeaf_InvalidSyntaxKind(
        IType type,
        SyntaxKind kind,
        Path path)
        => new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_FormatResultLeaf_InvalidSyntaxKind,
                    type.Print(),
                    kind)
                .SetInputPath(path)
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
                ThrowHelper_Schema_GetMember_DirectiveArgumentNotFound,
                coordinate.ArgumentName,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_DirectiveNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_Schema_GetMember_DirectiveNotFound,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_EnumValueNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_Schema_GetMember_EnumValueNotFound,
                coordinate.MemberName,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_InputFieldNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_Schema_GetMember_InputFieldNotFound,
                coordinate.MemberName,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_InvalidCoordinate(
        SchemaCoordinate coordinate,
        ITypeDefinition type)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_Schema_GetMember_InvalidCoordinate,
                coordinate.ToString(),
                type.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_FieldArgNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_Schema_GetMember_FieldArgNotFound,
                coordinate.ArgumentName,
                coordinate.Name,
                coordinate.MemberName),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_FieldNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_Schema_GetMember_FieldNotFound,
                coordinate.MemberName,
                coordinate.Name),
            coordinate);

    public static InvalidSchemaCoordinateException Schema_GetMember_TypeNotFound(
        SchemaCoordinate coordinate)
        => new InvalidSchemaCoordinateException(
            string.Format(
                CultureInfo.InvariantCulture,
                ThrowHelper_Schema_GetMember_TypeNotFound,
                coordinate.Name),
            coordinate);

    public static InvalidOperationException FieldBase_Sealed()
        => new(ThrowHelper_FieldBase_Sealed);

    public static InvalidOperationException NodeResolver_ArgumentTypeMissing()
        => new(ThrowHelper_NodeResolver_ArgumentTypeMissing);

    public static InvalidOperationException NodeResolver_ObjNoConfig()
        => new(ThrowHelper_NodeResolver_ObjNoDefinition);

    public static SchemaException RelayIdFieldHelpers_NoFieldType(
        string fieldName,
        TypeSystemObject? type = null)
    {
        var builder = SchemaErrorBuilder.New();
        builder.SetMessage(ThrowHelper_RelayIdFieldHelpers_NoFieldType, fieldName);

        if (type is not null)
        {
            builder.SetTypeSystemObject(type);
        }

        return new SchemaException(builder.Build());
    }

    public static GraphQLException MissingIfArgument(DirectiveNode directive)
        => new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_MissingDirectiveIfArgument,
                directive.Name.Value)
            .AddLocation(directive)
            .Build());

    public static InvalidOperationException Flags_Enum_Shape_Unknown(Type type)
        => new(string.Format(
            CultureInfo.InvariantCulture,
            ThrowHelper_Flags_Enum_Shape_Unknown,
            type.FullName ?? type.Name));

    public static GraphQLException Flags_Parser_NoSelection(InputObjectType type)
        => new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_Flags_Parser_NoSelection, type.Name)
            .Build());

    public static GraphQLException Flags_Parser_UnknownSelection(string value, InputObjectType type)
        => new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_Flags_Parser_UnknownSelection, value, type.Name)
            .Build());

    public static SchemaException Flags_IllegalFlagEnumName(Type type, string? valueName)
        => new(SchemaErrorBuilder.New()
            .SetMessage(
                ThrowHelper_Flags_IllegalFlagEnumName,
                type.FullName ?? type.Name,
                valueName ?? "value is null")
            .Build());

    public static SchemaException InputTypeExpected(IType type)
    {
        var namedType = type.NamedType();

        return new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_InputTypeExpected_Message, namedType.Name)
                .SetTypeSystemObject((TypeSystemObject)namedType)
                .SetExtension("type", type.Print())
                .Build());
    }

    public static SchemaException OutputTypeExpected(IType type)
    {
        var namedType = type.NamedType();

        return new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_OutputTypeExpected_Message, namedType.Name)
                .SetTypeSystemObject((TypeSystemObject)namedType)
                .SetExtension("type", type.Print())
                .Build());
    }

    public static LeafCoercionException InvalidTypeConversion(
        ITypeSystemMember type,
        IInputValueInfo inputField,
        Path inputFieldPath,
        Language.Location? location,
        Exception conversionException)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(ThrowHelper_InvalidTypeConversion, inputField.Name)
            .SetCode(ErrorCodes.Scalars.InvalidRuntimeType)
            .TryAddLocation(location)
            .SetException(conversionException)
            .SetCoordinate(inputField.Coordinate);

        if (inputFieldPath.Length > 1)
        {
            builder.SetInputPath(inputFieldPath);
        }

        return new(builder.Build(), type);
    }

    public static LeafCoercionException Scalar_Cannot_CoerceInputLiteral(
        ITypeDefinition scalarType,
        IValueNode? valueLiteral,
        Exception? error = null)
    {
        valueLiteral ??= NullValueNode.Default;

        var errorBuilder =
            ErrorBuilder.New()
                .SetMessage(
                    TypeResources.Scalar_Cannot_CoerceInputLiteral,
                    scalarType.Name,
                    valueLiteral.Kind);

        if (error is not null)
        {
            errorBuilder.SetException(error);
        }

        return new LeafCoercionException(
            errorBuilder.Build(),
            scalarType);
    }

    public static LeafCoercionException Scalar_Cannot_CoerceInputValue(
        ITypeDefinition scalarType,
        JsonElement inputValue,
        Exception? error = null)
    {
        var errorBuilder =
            ErrorBuilder.New()
                .SetMessage(
                    TypeResources.Scalar_Cannot_CoerceInputValue,
                    scalarType.Name,
                    inputValue.ValueKind);

        if (error is not null)
        {
            errorBuilder.SetException(error);
        }

        return new LeafCoercionException(
            errorBuilder.Build(),
            scalarType);
    }

    public static LeafCoercionException Scalar_FormatIsInvalid(
        ITypeDefinition scalarType,
        object runtimeValue)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    TypeResources.Scalar_FormatIsInvalid,
                    runtimeValue,
                    scalarType.Name)
                .Build(),
            scalarType);
    }

    public static LeafCoercionException Scalar_Cannot_ConvertValueToLiteral(
        ITypeDefinition scalarType,
        object runtimeValue)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    TypeResources.Scalar_Cannot_ConvertValueToLiteral,
                    scalarType.Name,
                    runtimeValue.GetType().FullName)
                .Build(),
            scalarType);
    }

    public static LeafCoercionException Scalar_Cannot_CoerceOutputValue(
        ITypeDefinition scalarType,
        object runtimeValue)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    TypeResources.Scalar_Cannot_CoerceOutputValue,
                    scalarType.Name,
                    runtimeValue.GetType().FullName)
                .Build(),
            scalarType);
    }

    public static LeafCoercionException RegexType_InvalidFormat(
        IType type,
        string name)
    {
        return new LeafCoercionException(
            ErrorBuilder.New()
                .SetMessage(
                    string.Format(
                        TypeResources.RegexType_InvalidFormat,
                        name))
                .SetCode(ErrorCodes.Scalars.InvalidSyntaxFormat)
                .Build(),
            type);
    }
}
