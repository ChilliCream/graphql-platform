using HotChocolate.Types.Analyzers.Properties;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers;

public static class Errors
{
    public static readonly DiagnosticDescriptor KeyParameterMissing =
        new(
            id: "HC0074",
            title: "Parameter Missing",
            messageFormat: SourceGenResources.DataLoader_KeyParameterMissing,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodAccessModifierInvalid =
        new(
            id: "HC0075",
            title: "Access Modifier Invalid",
            messageFormat: SourceGenResources.DataLoader_InvalidAccessModifier,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ObjectTypePartialKeywordMissing =
        new(
            id: "HC0080",
            title: "Partial Keyword Missing",
            messageFormat: "A split object type class needs to be a partial class",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ObjectTypeStaticKeywordMissing =
        new(
            id: "HC0081",
            title: "Static Keyword Missing",
            messageFormat: "A split object type class needs to be a static class",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InterfaceTypePartialKeywordMissing =
        new(
            id: "HC0080",
            title: "Partial Keyword Missing",
            messageFormat: "A split object type class needs to be a partial class",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InterfaceTypeStaticKeywordMissing =
        new(
            id: "HC0081",
            title: "Static Keyword Missing",
            messageFormat: "A split object type class needs to be a static class",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TooManyNodeResolverArguments =
        new(
            id: "HC0083",
            title: "Too Many Arguments",
            messageFormat: "A node resolver can only have a single field argument called `id`",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidNodeResolverArgumentName =
        new(
            id: "HC0084",
            title: "Invalid Argument Name",
            messageFormat: "A node resolver can only have a single field argument called `id`",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataLoaderCannotBeGeneric =
        new(
            id: "HC0085",
            title: "DataLoader Cannot Be Generic",
            messageFormat: "The DataLoader source generator cannot generate generic DataLoaders",
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConnectionSingleGenericTypeArgument =
        new(
            id: "HC0086",
            title: "Invalid Connection Structure",
            messageFormat: "A generic connection/edge type must have a single generic type argument that represents the node type",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConnectionNameFormatIsInvalid =
        new(
            id: "HC0087",
            title: "Invalid Connection/Edge Name Format",
            messageFormat: "A connection/edge name must be in the format `{0}Edge` or `{0}Connection`",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConnectionNameDuplicate =
        new(
            id: "HC0088",
            title: "Invalid Connection/Edge Name",
            messageFormat: "The type `{0}` cannot be mapped to the GraphQL type name `{1}` as `{2}` is already mapped to it",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RootTypePartialKeywordMissing =
        new(
            id: "HC0091",
            title: "Partial Keyword Missing",
            messageFormat: "A static root type class should be declared as partial to allow source generation",
            category: "TypeSystem",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NodeResolverIdAttributeNotAllowed =
        new(
            id: "HC0092",
            title: "ID Attribute Not Allowed",
            messageFormat: "The [ID] attribute should not be used on node resolver parameters as the NodeResolver attribute already declares the parameter as an ID type",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NodeResolverMustBePublic =
        new(
            id: "HC0093",
            title: "Node Resolver Must Be Public",
            messageFormat: "A node resolver method must be public",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BindMemberNotFound =
        new(
            id: "HC0094",
            title: "Bind Member Not Found",
            messageFormat: "The member '{0}' does not exist on type '{1}'",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BindMemberTypeMismatch =
        new(
            id: "HC0095",
            title: "Bind Member Type Mismatch",
            messageFormat: "The type '{0}' in nameof expression does not match the ObjectType type '{1}'",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExtendObjectTypeShouldBeUpgraded =
        new(
            id: "HC0096",
            title: "ExtendObjectType Should Be Upgraded",
            messageFormat: "Consider upgrading [ExtendObjectType<{0}>] to [ObjectType<{0}>]",
            category: "TypeSystem",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParentAttributeTypeMismatch =
        new(
            id: "HC0097",
            title: "Parent Attribute Type Mismatch",
            messageFormat: "The parameter type '{0}' must be '{1}' or a base type/interface that '{1}' implements",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParentMethodTypeMismatch =
        new(
            id: "HC0098",
            title: "Parent Method Type Mismatch",
            messageFormat: "The type argument '{0}' in Parent<T>() must be '{1}' or a base type/interface that '{1}' implements",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QueryContextWithUseProjection =
        new(
            id: "HC0099",
            title: "QueryContext With UseProjection",
            messageFormat: "Methods with QueryContext<T> parameters cannot use the [UseProjection] attribute",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataAttributeOrder =
        new(
            id: "HC0100",
            title: "Data Attribute Order",
            messageFormat: "Data attributes must be ordered correctly: [UsePaging], [UseProjection], [UseFiltering], [UseSorting]",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QueryContextConnectionMismatch =
        new(
            id: "HC0101",
            title: "QueryContext Generic Type Mismatch",
            messageFormat: "The QueryContext<{0}> parameter must match the connection node type {1}",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ShareableOnInterfaceType =
        new(
            id: "HC0102",
            title: "Shareable Not Allowed On Interface Type",
            messageFormat: "The [Shareable] attribute is not allowed on classes decorated with [InterfaceType<T>]",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ShareableScopedOnMember =
        new(
            id: "HC0103",
            title: "Shareable Scoped Not Allowed On Members",
            messageFormat: "The [Shareable] attribute on properties and methods must not specify the 'scoped' argument",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NodeResolverIdParameter =
        new(
            id: "HC0104",
            title: "NodeResolver First Parameter Must Be Named 'id'",
            messageFormat: "The first parameter of a node resolver must be the node id and must be called 'id'",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
