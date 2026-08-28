using HotChocolate.Types.Analyzers.Properties;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers;

public static class Errors
{
    public static readonly DiagnosticDescriptor KeyParameterMissing =
        new(
            id: ErrorCodes.Analyzers.KeyParameterMissing,
            title: "Parameter Missing",
            messageFormat: SourceGenResources.DataLoader_KeyParameterMissing,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodAccessModifierInvalid =
        new(
            id: ErrorCodes.Analyzers.MethodAccessModifierInvalid,
            title: "Access Modifier Invalid",
            messageFormat: SourceGenResources.DataLoader_InvalidAccessModifier,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ObjectTypePartialKeywordMissing =
        new(
            id: ErrorCodes.Analyzers.ObjectTypePartialKeywordMissing,
            title: "Partial Keyword Missing",
            messageFormat: "A split object type class needs to be a partial class",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ObjectTypeStaticKeywordMissing =
        new(
            id: ErrorCodes.Analyzers.ObjectTypeStaticKeywordMissing,
            title: "Static Keyword Missing",
            messageFormat: "A split object type class needs to be a static class",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TooManyNodeResolverArguments =
        new(
            id: ErrorCodes.Analyzers.TooManyNodeResolverArguments,
            title: "Too Many Arguments",
            messageFormat: "A node resolver can only have a single field argument called `id`",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidNodeResolverArgumentName =
        new(
            id: ErrorCodes.Analyzers.InvalidNodeResolverArgumentName,
            title: "Invalid Argument Name",
            messageFormat: "A node resolver can only have a single field argument called `id`",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RootTypePartialKeywordMissing =
        new(
            id: ErrorCodes.Analyzers.RootTypePartialKeywordMissing,
            title: "Partial Keyword Missing",
            messageFormat: "A root type class should be declared as partial to allow source generation",
            category: "TypeSystem",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NodeResolverIdAttributeNotAllowed =
        new(
            id: ErrorCodes.Analyzers.NodeResolverIdAttributeNotAllowed,
            title: "ID Attribute Not Allowed",
            messageFormat: "The [ID] attribute should not be used on node resolver parameters as the NodeResolver attribute already declares the parameter as an ID type",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NodeResolverMustBePublic =
        new(
            id: ErrorCodes.Analyzers.NodeResolverMustBePublic,
            title: "Node Resolver Must Be Public",
            messageFormat: "A node resolver method must be public",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BindMemberNotFound =
        new(
            id: ErrorCodes.Analyzers.BindMemberNotFound,
            title: "Bind Member Not Found",
            messageFormat: "The member '{0}' does not exist on type '{1}'",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BindMemberTypeMismatch =
        new(
            id: ErrorCodes.Analyzers.BindMemberTypeMismatch,
            title: "Bind Member Type Mismatch",
            messageFormat: "The type '{0}' in nameof expression does not match the ObjectType type '{1}'",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExtendObjectTypeShouldBeUpgraded =
        new(
            id: ErrorCodes.Analyzers.ExtendObjectTypeShouldBeUpgraded,
            title: "ExtendObjectType Should Be Upgraded",
            messageFormat: "Consider upgrading [ExtendObjectType<{0}>] to [ObjectType<{0}>]",
            category: "TypeSystem",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParentAttributeTypeMismatch =
        new(
            id: ErrorCodes.Analyzers.ParentAttributeTypeMismatch,
            title: "Parent Attribute Type Mismatch",
            messageFormat: "The parameter type '{0}' must be '{1}' or a base type/interface that '{1}' implements",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParentMethodTypeMismatch =
        new(
            id: ErrorCodes.Analyzers.ParentMethodTypeMismatch,
            title: "Parent Method Type Mismatch",
            messageFormat: "The type argument '{0}' in Parent<T>() must be '{1}' or a base type/interface that '{1}' implements",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QueryContextWithUseProjection =
        new(
            id: ErrorCodes.Analyzers.QueryContextWithUseProjection,
            title: "QueryContext With UseProjection",
            messageFormat: "Methods with QueryContext<T> parameters cannot use the [UseProjection] attribute",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataAttributeOrder =
        new(
            id: ErrorCodes.Analyzers.DataAttributeOrder,
            title: "Data Attribute Order",
            messageFormat: "Data attributes must be ordered correctly: [UsePaging], [UseProjection], [UseFiltering], [UseSorting]",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QueryContextConnectionMismatch =
        new(
            id: ErrorCodes.Analyzers.QueryContextConnectionMismatch,
            title: "QueryContext Generic Type Mismatch",
            messageFormat: "The QueryContext<{0}> parameter must match the connection node type {1}",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ShareableOnInterfaceType =
        new(
            id: ErrorCodes.Analyzers.ShareableOnInterfaceType,
            title: "Shareable Not Allowed On Interface Type",
            messageFormat: "The [Shareable] attribute is not allowed on classes decorated with [InterfaceType<T>]",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ShareableScopedOnMember =
        new(
            id: ErrorCodes.Analyzers.ShareableScopedOnMember,
            title: "Shareable Scoped Not Allowed On Members",
            messageFormat: "The [Shareable] attribute on properties and methods must not specify the 'scoped' argument",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NodeResolverIdParameter =
        new(
            id: ErrorCodes.Analyzers.NodeResolverIdParameter,
            title: "NodeResolver First Parameter Must Be Named 'id'",
            messageFormat: "The first parameter of a node resolver must be the node ID and must be named 'id'",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IdAttributeOnRecordParameter =
        new(
            id: ErrorCodes.Analyzers.IdAttributeOnRecordParameter,
            title: "ID Attribute Must Target Property",
            messageFormat: "The [ID] attribute on record parameters must use the 'property:' target specifier",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WrongAuthorizeAttribute =
        new(
            id: ErrorCodes.Analyzers.WrongAuthorizeAttribute,
            title: "Microsoft Authorization Attribute Not Allowed",
            messageFormat: "Use HotChocolate.Authorization.{0} instead",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InterfaceTypePartialKeywordMissing =
        new(
            id: ErrorCodes.Analyzers.InterfaceTypePartialKeywordMissing,
            title: "Partial Keyword Missing",
            messageFormat: "A split interface type class needs to be a partial class",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConnectionNameDuplicate =
        new(
            id: ErrorCodes.Analyzers.ConnectionNameDuplicate,
            title: "Invalid Connection/Edge Name",
            messageFormat: "The type `{0}` cannot be mapped to the GraphQL type name `{1}` as `{2}` is already mapped to it",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConnectionNameFormatIsInvalid =
        new(
            id: ErrorCodes.Analyzers.ConnectionNameFormatIsInvalid,
            title: "Invalid Connection/Edge Name Format",
            messageFormat: "A connection/edge name must be in the format `{0}Edge` or `{0}Connection`",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConnectionSingleGenericTypeArgument =
        new(
            id: ErrorCodes.Analyzers.ConnectionSingleGenericTypeArgument,
            title: "Invalid Connection Structure",
            messageFormat: "A generic connection/edge type must have a single generic type argument that represents the node type",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderCannotBeGeneric =
        new(
            id: ErrorCodes.Analyzers.DataLoaderCannotBeGeneric,
            title: "DataLoader Cannot Be Generic",
            messageFormat: "The DataLoader source generator cannot generate generic DataLoaders",
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderTypeInvalid =
        new(
            id: ErrorCodes.Analyzers.DataLoaderTypeInvalid,
            title: "Invalid DataLoader Type",
            messageFormat: SourceGenResources.DataLoader_TypeInvalid,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderKeyParameterInvalid =
        new(
            id: ErrorCodes.Analyzers.DataLoaderKeyParameterInvalid,
            title: "Invalid DataLoader Key Parameter",
            messageFormat: SourceGenResources.DataLoader_KeyParameterInvalid,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderReturnTypeInvalid =
        new(
            id: ErrorCodes.Analyzers.DataLoaderReturnTypeInvalid,
            title: "Invalid DataLoader Return Type",
            messageFormat: SourceGenResources.DataLoader_ReturnTypeInvalid,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderMultipleAttributes =
        new(
            id: ErrorCodes.Analyzers.DataLoaderMultipleAttributes,
            title: "Multiple DataLoader Attributes",
            messageFormat: SourceGenResources.DataLoader_MultipleAttributes,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderDuplicateType =
        new(
            id: ErrorCodes.Analyzers.DataLoaderDuplicateType,
            title: "Duplicate DataLoader Type",
            messageFormat: SourceGenResources.DataLoader_DuplicateType,
            category: "DataLoader",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor DataLoaderMissingInterfaceImplementation =
        new(
            id: ErrorCodes.Analyzers.DataLoaderMissingInterfaceImplementation,
            title: "Missing DataLoader Interface Implementation",
            messageFormat: SourceGenResources.DataLoader_MissingInterfaceImplementation,
            category: "DataLoader",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderPublicInterfaceAccessModifier =
        new(
            id: ErrorCodes.Analyzers.DataLoaderPublicInterfaceAccessModifier,
            title: "Public Interface Access Modifier Ignored",
            messageFormat: SourceGenResources.DataLoader_PublicInterfaceAccessModifier,
            category: "DataLoader",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderParameterModifierInvalid =
        new(
            id: ErrorCodes.Analyzers.DataLoaderParameterModifierInvalid,
            title: "Invalid DataLoader Parameter Modifier",
            messageFormat: SourceGenResources.DataLoader_ParameterModifierInvalid,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InterfaceTypeStaticKeywordMissing =
        new(
            id: ErrorCodes.Analyzers.InterfaceTypeStaticKeywordMissing,
            title: "Static Keyword Missing",
            messageFormat: "A split interface type class needs to be a static class",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LookupReturnsNonNullableType =
        new(
            id: ErrorCodes.Analyzers.LookupReturnsNonNullableType,
            title: "Lookup Must Return Nullable Type",
            messageFormat: "A method or property with the [Lookup] attribute must return a nullable type",
            category: "TypeSystem",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LookupReturnsListType =
        new(
            id: ErrorCodes.Analyzers.LookupReturnsListType,
            title: "Lookup Must Not Return List Type",
            messageFormat: "A method or property with the [Lookup] attribute must not return a list type",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
