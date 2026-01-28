using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class CompilationExtensions
{
    public static bool TryGetConnectionNameFromResolver(
        this Compilation compilation,
        ISymbol resolver,
        [NotNullWhen(true)] out string? name)
    {
        var useConnectionAttribute = compilation.GetTypeByMetadataName(WellKnownAttributes.UseConnectionAttribute);

        if (useConnectionAttribute is null)
        {
            name = null;
            return false;
        }

        const string connectionName = "Name";
        const string connection = "Connection";

        foreach (var attributeData in resolver.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, useConnectionAttribute))
            {
                continue;
            }

            foreach (var namedArg in attributeData.NamedArguments)
            {
                if (namedArg is { Key: connectionName, Value.Value: string namedValue })
                {
                    if (namedValue.EndsWith(connection))
                    {
#if NET8_0_OR_GREATER
                        namedValue = namedValue[..^connection.Length];
#else
                        namedValue = namedValue.Substring(0, namedValue.Length - connection.Length);
#endif
                    }

                    name = namedValue;
                    return true;
                }
            }
        }

        name = null;
        return false;
    }

    public static bool TryGetGraphQLTypeName(
        this Compilation compilation,
        ISymbol symbol,
        [NotNullWhen(true)] out string? name)
    {
        var graphQLNameAttribute = compilation.GetTypeByMetadataName(WellKnownAttributes.GraphQLNameAttribute);

        if (graphQLNameAttribute is null)
        {
            name = null;
            return false;
        }

        foreach (var attributeData in symbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, graphQLNameAttribute))
            {
                continue;
            }

            if (attributeData.ConstructorArguments.Length > 0
                && attributeData.ConstructorArguments[0].Value is string attributeValue)
            {
                name = attributeValue;
                return true;
            }
        }

        name = null;
        return false;
    }

    public static IMemberDescription? GetDescription(
        this Compilation compilation,
        ISymbol symbol)
    {
        switch (symbol)
        {
            case IPropertySymbol property:
                return property.GetDescription(compilation);

            case IMethodSymbol method:
                return method.GetDescription(compilation);

            case IParameterSymbol parameter:
                return parameter.GetDescription(compilation);

            default:
                return null;
        }
    }

    public static string? GetDeprecationReason(this Compilation compilation, ISymbol symbol)
    {
        var graphQLDeprecatedAttribute = compilation.GetTypeByMetadataName(WellKnownAttributes.GraphQLDeprecatedAttribute);
        var obsoleteAttribute = compilation.GetTypeByMetadataName(WellKnownAttributes.ObsoleteAttribute);

        const string defaultReason = "No longer supported.";

        foreach (var attributeData in symbol.GetAttributes())
        {
            // Check for GraphQLDeprecatedAttribute
            if (graphQLDeprecatedAttribute is not null
                && SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, graphQLDeprecatedAttribute))
            {
                if (attributeData.ConstructorArguments.Length > 0
                    && attributeData.ConstructorArguments[0].Value is string deprecatedReason
                    && !string.IsNullOrWhiteSpace(deprecatedReason))
                {
                    return deprecatedReason;
                }

                return defaultReason;
            }

            // Check for ObsoleteAttribute
            if (obsoleteAttribute is not null
                && SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, obsoleteAttribute))
            {
                if (attributeData.ConstructorArguments.Length > 0
                    && attributeData.ConstructorArguments[0].Value is string obsoleteReason
                    && !string.IsNullOrWhiteSpace(obsoleteReason))
                {
                    return obsoleteReason;
                }

                return defaultReason;
            }
        }

        return null;
    }

    public static INamedTypeSymbol? GetConnectionBaseSymbol(this GeneratorSyntaxContext context)
        => context.SemanticModel.Compilation.GetConnectionBaseSymbol();

    public static INamedTypeSymbol? GetFieldResultInterface(this Compilation compilation)
        => compilation.GetTypeByMetadataName("HotChocolate.IFieldResult");

    public static INamedTypeSymbol? GetConnectionBaseSymbol(this Compilation compilation)
        => compilation.GetTypeByMetadataName("HotChocolate.Types.Pagination.ConnectionBase`3");

    public static INamedTypeSymbol? GetEdgeInterfaceSymbol(this Compilation compilation)
        => compilation.GetTypeByMetadataName("HotChocolate.Types.Pagination.IEdge`1");

    public static INamedTypeSymbol? GetTaskSymbol(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

    public static INamedTypeSymbol? GetValueTaskSymbol(this Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

    public static INamedTypeSymbol? GetConnectionFlagsSymbol(this Compilation compilation)
        => compilation.GetTypeByMetadataName("HotChocolate.Types.Pagination.ConnectionFlags");

    public static bool IsConnectionType(this GeneratorSyntaxContext context, ITypeSymbol possibleConnectionType)
        => context.SemanticModel.Compilation.IsConnectionType(possibleConnectionType);

    public static bool IsEdgeType(this GeneratorSyntaxContext context, ITypeSymbol possibleEdgeType)
        => context.SemanticModel.Compilation.IsEdgeType(possibleEdgeType);

    public static bool IsTaskOrValueTask(
        this Compilation compilation,
        ITypeSymbol possibleTask,
        [NotNullWhen(true)] out ITypeSymbol? innerType)
    {
        if (possibleTask is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var taskSymbol = compilation.GetTaskSymbol();
            var valueTaskSymbol = compilation.GetValueTaskSymbol();

            if (SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, taskSymbol)
                || SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, valueTaskSymbol))
            {
                innerType = namedType.TypeArguments[0];
                return true;
            }
        }

        innerType = null;
        return false;
    }

    public static bool IsConnectionType(this Compilation compilation, ITypeSymbol possibleConnectionType)
    {
        if (compilation.IsTaskOrValueTask(possibleConnectionType, out var innerType))
        {
            possibleConnectionType = innerType;
        }

        if (possibleConnectionType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        return IsDerivedFromGenericBase(namedType, compilation.GetConnectionBaseSymbol());
    }

    public static bool IsEdgeType(this Compilation compilation, ITypeSymbol possibleEdgeType)
    {
        if (compilation.IsTaskOrValueTask(possibleEdgeType, out var innerType))
        {
            possibleEdgeType = innerType;
        }

        if (possibleEdgeType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        return IsDerivedFromGenericBase(namedType, compilation.GetEdgeInterfaceSymbol());
    }

    public static bool IsConnectionFlagsType(this Compilation compilation, ITypeSymbol possibleConnectionFlagsType)
    {
        if (possibleConnectionFlagsType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(namedType, compilation.GetConnectionFlagsSymbol());
    }

    private static bool IsDerivedFromGenericBase(INamedTypeSymbol? typeSymbol, INamedTypeSymbol? baseTypeSymbol)
    {
        // if we are generating only for GreenDonut some base types might not exist.
        if (baseTypeSymbol is null)
        {
            return false;
        }

        // If baseTypeSymbol is an interface, check all implemented interfaces
        if (baseTypeSymbol.TypeKind == TypeKind.Interface)
        {
            return typeSymbol?.AllInterfaces.Any(
                i => i.IsGenericType
                    && SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, baseTypeSymbol)) ?? false;
        }

        // Otherwise, walk up the base type hierarchy
        var current = typeSymbol;

        while (current is not null)
        {
            if (current is { IsGenericType: true } namedTypeSymbol)
            {
                var baseType = namedTypeSymbol.ConstructedFrom;
                if (SymbolEqualityComparer.Default.Equals(baseType, baseTypeSymbol))
                {
                    return true;
                }
            }

            current = current.BaseType;
        }

        return false;
    }

    public static ResolverParameterKind GetParameterKind(
        this Compilation compilation,
        IParameterSymbol parameter,
        out string? key)
    {
        key = null;

        if (parameter.IsParent())
        {
            return ResolverParameterKind.Parent;
        }

        if (parameter.IsCancellationToken())
        {
            return ResolverParameterKind.CancellationToken;
        }

        if (parameter.IsClaimsPrincipal())
        {
            return ResolverParameterKind.ClaimsPrincipal;
        }

        if (parameter.IsDocumentNode())
        {
            return ResolverParameterKind.DocumentNode;
        }

        if (parameter.IsEventMessage())
        {
            return ResolverParameterKind.EventMessage;
        }

        if (parameter.IsFieldNode())
        {
            return ResolverParameterKind.FieldNode;
        }

        if (parameter.IsOutputField(compilation))
        {
            return ResolverParameterKind.OutputField;
        }

        if (parameter.IsHttpContext())
        {
            return ResolverParameterKind.HttpContext;
        }

        if (parameter.IsHttpRequest())
        {
            return ResolverParameterKind.HttpRequest;
        }

        if (parameter.IsHttpResponse())
        {
            return ResolverParameterKind.HttpResponse;
        }

        if (parameter.IsGlobalState(out key))
        {
            return parameter.IsSetState()
                ? ResolverParameterKind.SetGlobalState
                : ResolverParameterKind.GetGlobalState;
        }

        if (parameter.IsScopedState(out key))
        {
            return parameter.IsSetState()
                ? ResolverParameterKind.SetScopedState
                : ResolverParameterKind.GetScopedState;
        }

        if (parameter.IsLocalState(out key))
        {
            return parameter.IsSetState()
                ? ResolverParameterKind.SetLocalState
                : ResolverParameterKind.GetLocalState;
        }

        if (parameter.IsService(out key))
        {
            return ResolverParameterKind.Service;
        }

        if (parameter.IsArgument(out key))
        {
            return ResolverParameterKind.Argument;
        }

        if (parameter.IsQueryContext())
        {
            return ResolverParameterKind.QueryContext;
        }

        if (parameter.IsPagingArguments())
        {
            return ResolverParameterKind.PagingArguments;
        }

        if (compilation.IsConnectionFlagsType(parameter.Type))
        {
            return ResolverParameterKind.ConnectionFlags;
        }

        return ResolverParameterKind.Unknown;
    }
}
