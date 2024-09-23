using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class SymbolExtensions
{
    public static bool IsNullableType(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.IsNullableRefType() ||
            typeSymbol.IsNullableValueType();
    }

    public static bool IsNullableRefType(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.IsReferenceType
            && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }

    public static bool IsNullableValueType(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.IsGenericType &&
                namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return true;
            }
        }

        return false;
    }

    public static string PrintNullRefQualifier(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.IsNullableRefType() ? "?" : string.Empty;
    }

    public static string ToFullyQualified(this ITypeSymbol typeSymbol)
        => typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static bool IsParent(this IParameterSymbol parameter)
        => parameter.IsThis ||
            parameter
                .GetAttributes()
                .Any(static t => t.AttributeClass?.ToDisplayString() == WellKnownAttributes.ParentAttribute);

    public static bool IsCancellationToken(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.CancellationToken;

    public static bool IsClaimsPrincipal(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.ClaimsPrincipal;

    public static bool IsDocumentNode(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.DocumentNode;

    public static bool IsFieldNode(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.FieldNode;

    public static bool IsOutputField(this IParameterSymbol parameterSymbol, Compilation compilation)
    {
        var type = compilation.GetTypeByMetadataName(WellKnownTypes.OutputField);
        return type != null && compilation.ClassifyConversion(parameterSymbol.Type, type).IsImplicit;
    }

    public static bool IsHttpContext(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.HttpContext;

    public static bool IsHttpRequest(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.HttpRequest;

    public static bool IsHttpResponse(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.HttpResponse;

    public static bool IsSetState(this IParameterSymbol parameter, [NotNullWhen(true)] out string? stateTypeName)
    {
        if (parameter.Type is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 })
            {
                if (namedTypeSymbol.Name == "SetState" &&
                    namedTypeSymbol.ContainingNamespace.ToDisplayString() == "HotChocolate")
                {
                    stateTypeName = namedTypeSymbol.TypeArguments[0].ToDisplayString();
                    return true;
                }
            }
        }

        stateTypeName = null;
        return false;
    }

    public static bool IsSetState(this IParameterSymbol parameter)
    {
        if (parameter.Type is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 })
            {
                if (namedTypeSymbol.Name == "SetState" &&
                    namedTypeSymbol.ContainingNamespace.ToDisplayString() == "HotChocolate")
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsGlobalState(
        this IParameterSymbol parameter,
        [NotNullWhen(true)] out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == "HotChocolate.GlobalStateAttribute")
            {
                if (attributeData.ConstructorArguments.Length == 1 &&
                    attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                    attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg.Key == "Key" && namedArg.Value.Value is string namedKeyValue)
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = parameter.Name;
                return true;
            }
        }

        return false;
    }

    public static bool IsScopedState(
        this IParameterSymbol parameter,
        [NotNullWhen(true)] out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == "HotChocolate.ScopedStateAttribute")
            {
                if (attributeData.ConstructorArguments.Length == 1 &&
                    attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                    attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg is { Key: "Key", Value.Value: string namedKeyValue })
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = parameter.Name;
                return true;
            }
        }

        return false;
    }

    public static bool IsLocalState(
        this IParameterSymbol parameter,
        [NotNullWhen(true)] out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == "HotChocolate.LocalStateAttribute")
            {
                if (attributeData.ConstructorArguments.Length == 1 &&
                    attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                    attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg.Key == "Key" && namedArg.Value.Value is string namedKeyValue)
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = parameter.Name;
                return true;
            }
        }

        return false;
    }

    public static bool IsEventMessage(
        this IParameterSymbol parameter)
    {
        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == WellKnownAttributes.EventMessageAttribute)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsService(
        this IParameterSymbol parameter,
        out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == WellKnownAttributes.ServiceAttribute)
            {
                if (attributeData.ConstructorArguments.Length == 1 &&
                    attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                    attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg is { Key: "Key", Value.Value: string namedKeyValue })
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = null;
                return true;
            }
        }

        return false;
    }

    public static bool IsArgument(
        this IParameterSymbol parameter,
        out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == WellKnownAttributes.ArgumentAttribute)
            {
                if (attributeData.ConstructorArguments.Length == 1 &&
                    attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                    attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg is { Key: "Name", Value.Value: string namedKeyValue })
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = null;
                return true;
            }
        }

        return false;
    }

    public static bool IsNonNullable(this IParameterSymbol parameter)
    {
        if (parameter.Type.NullableAnnotation != NullableAnnotation.NotAnnotated)
        {
            return false;
        }

        if (parameter.Type is INamedTypeSymbol namedTypeSymbol &&
            namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return false;
        }

        return true;
    }

    public static ResolverResultKind GetResultKind(this IMethodSymbol method)
    {
        const string task = $"{WellKnownTypes.Task}<";
        const string valueTask = $"{WellKnownTypes.ValueTask}<";
        const string taskEnumerable = $"{WellKnownTypes.Task}<{WellKnownTypes.AsyncEnumerable}<";
        const string valueTaskEnumerable = $"{WellKnownTypes.ValueTask}<{WellKnownTypes.AsyncEnumerable}<";

        if (method.ReturnsVoid || method.ReturnsByRef || method.ReturnsByRefReadonly)
        {
            return ResolverResultKind.Invalid;
        }

        var returnType = method.ReturnType.ToDisplayString();

        if (returnType.Equals(WellKnownTypes.Task) ||
            returnType.Equals(WellKnownTypes.ValueTask))
        {
            return ResolverResultKind.Invalid;
        }

        if (returnType.StartsWith(task) ||
            returnType.StartsWith(valueTask))
        {
            if (returnType.StartsWith(taskEnumerable) ||
                returnType.StartsWith(valueTaskEnumerable))
            {
                return ResolverResultKind.TaskAsyncEnumerable;
            }

            return ResolverResultKind.Task;
        }

        if (returnType.StartsWith(WellKnownTypes.Executable))
        {
            return ResolverResultKind.Executable;
        }

        if (returnType.StartsWith(WellKnownTypes.Queryable))
        {
            return ResolverResultKind.Queryable;
        }

        if (returnType.StartsWith(WellKnownTypes.AsyncEnumerable))
        {
            return ResolverResultKind.AsyncEnumerable;
        }

        return ResolverResultKind.Pure;
    }

    public static bool IsListType(this ISymbol member, [NotNullWhen(true)] out string? elementType)
    {
        if (member is IMethodSymbol methodSymbol)
        {
            return methodSymbol.ReturnType.IsListType(out elementType);
        }

        if (member is IPropertySymbol propertySymbol)
        {
            return propertySymbol.Type.IsListType(out elementType);
        }

        elementType = null;
        return false;
    }

    public static bool IsListType(this ITypeSymbol typeSymbol, [NotNullWhen(true)] out string? elementType)
    {
        typeSymbol = UnwrapWrapperTypes(typeSymbol);

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
        {
            var typeDefinition = namedTypeSymbol.ConstructUnboundGenericType().ToDisplayString();

            if (WellKnownTypes.SupportedListInterfaces.Contains(typeDefinition))
            {
                elementType = namedTypeSymbol.TypeArguments[0].ToFullyQualified();
                return true;
            }

            if(typeDefinition.Equals(WellKnownTypes.EnumerableDefinition, StringComparison.Ordinal))
            {
                elementType = namedTypeSymbol.TypeArguments[0].ToFullyQualified();
                return true;
            }

            foreach (var interfaceType in namedTypeSymbol.AllInterfaces)
            {
                if (interfaceType.IsGenericType)
                {
                    var interfaceTypeDefinition = interfaceType.ConstructUnboundGenericType().ToDisplayString();
                    if (WellKnownTypes.SupportedListInterfaces.Contains(interfaceTypeDefinition))
                    {
                        elementType = interfaceType.TypeArguments[0].ToFullyQualified();
                        return true;
                    }
                }
            }
        }

        elementType = null;
        return false;
    }

    private static ITypeSymbol UnwrapWrapperTypes(ITypeSymbol typeSymbol)
    {
        while (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            var typeDefinition = namedTypeSymbol.ConstructUnboundGenericType().ToDisplayString();
            if (WellKnownTypes.TaskWrapper.Contains(typeDefinition))
            {
                typeSymbol = namedTypeSymbol.TypeArguments[0];
            }
            else
            {
                break;
            }
        }
        return typeSymbol;
    }

    public static bool HasPostProcessorAttribute(this ISymbol member)
    {
        foreach (var attributeData in member.GetAttributes())
        {
            if (IsPostProcessorAttribute(attributeData.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPostProcessorAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        while (attributeClass != null)
        {
            var typeName = attributeClass.ToDisplayString();
            if (typeName.Equals("HotChocolate.Types.UsePagingAttribute") ||
                typeName.Equals("HotChocolate.Types.UseOffsetPagingAttribute"))
            {
                return true;
            }

            if (attributeClass.IsGenericType)
            {
                var typeDefinition = attributeClass.ConstructUnboundGenericType().ToDisplayString();
                if (typeDefinition == "HotChocolate.Types.UseResolverResultPostProcessorAttribute<>")
                {
                    return true;
                }
            }

            attributeClass = attributeClass.BaseType;
        }

        return false;
    }
}
