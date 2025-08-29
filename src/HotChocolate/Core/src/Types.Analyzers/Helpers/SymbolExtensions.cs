using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;
using static Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat s_format =
        FullyQualifiedFormat.AddMiscellaneousOptions(
            IncludeNullableReferenceTypeModifier);

    public static bool IsNullableType(this ITypeSymbol typeSymbol)
        => typeSymbol.IsNullableRefType() || typeSymbol.IsNullableValueType();

    public static bool IsNullableRefType(this ITypeSymbol typeSymbol)
        => typeSymbol is
        {
            IsReferenceType: true,
            NullableAnnotation: NullableAnnotation.Annotated
        };

    public static bool IsNullableValueType(this ITypeSymbol typeSymbol)
        => typeSymbol is INamedTypeSymbol
        {
            IsGenericType: true,
            OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
        };

    public static string PrintNullRefQualifier(this ITypeSymbol typeSymbol)
        => typeSymbol.IsNullableRefType() ? "?" : string.Empty;

    public static string ToFullyQualified(this ITypeSymbol typeSymbol)
        => typeSymbol.ToDisplayString(FullyQualifiedFormat);

    public static string ToFullyQualifiedWithNullRefQualifier(this ITypeSymbol typeSymbol)
        => typeSymbol.ToDisplayString(s_format);

    public static string ToNullableFullyQualifiedWithNullRefQualifier(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsValueType)
        {
            return typeSymbol.ToFullyQualifiedWithNullRefQualifier();
        }

        var value = typeSymbol.ToFullyQualifiedWithNullRefQualifier();
        return value[value.Length - 1] != '?' ? value + "?" : value;
    }

    public static string ToClassNonNullableFullyQualifiedWithNullRefQualifier(this ITypeSymbol typeSymbol)
    {
        var value = typeSymbol.ToFullyQualifiedWithNullRefQualifier();
        return !typeSymbol.IsValueType && value[value.Length - 1] == '?'
            ? value.Substring(0, value.Length - 1)
            : value;
    }

    public static bool IsParent(this IParameterSymbol parameter)
        => parameter.IsThis
            || parameter
                .GetAttributes()
                .Any(static t => t.AttributeClass?.ToDisplayString() == WellKnownAttributes.ParentAttribute);

    public static bool IsIgnored(this ISymbol member)
        => member.GetAttributes()
            .Any(static t => t.AttributeClass?.ToDisplayString() == WellKnownAttributes.GraphQLIgnoreAttribute);

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
        if (parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 }
            && namedTypeSymbol.Name == "SetState"
            && namedTypeSymbol.ContainingNamespace.ToDisplayString() == "HotChocolate")
        {
            stateTypeName = namedTypeSymbol.TypeArguments[0].ToDisplayString();
            return true;
        }

        stateTypeName = null;
        return false;
    }

    public static bool IsSetState(this IParameterSymbol parameter)
        => parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 }
            && namedTypeSymbol.Name == "SetState"
            && namedTypeSymbol.ContainingNamespace.ToDisplayString() == "HotChocolate";

    public static bool IsQueryContext(this IParameterSymbol parameter)
        => parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 }
            && namedTypeSymbol.ToDisplayString().StartsWith(WellKnownTypes.QueryContextGeneric);

    public static bool IsPagingArguments(this IParameterSymbol parameter)
        => parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol.ToDisplayString().StartsWith(WellKnownTypes.PagingArguments);

    public static bool IsGlobalState(
        this IParameterSymbol parameter,
        [NotNullWhen(true)] out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (IsOrInheritsFrom(attributeData.AttributeClass, "HotChocolate.GlobalStateAttribute"))
            {
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
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

    public static bool IsScopedState(
        this IParameterSymbol parameter,
        [NotNullWhen(true)] out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (IsOrInheritsFrom(attributeData.AttributeClass, "HotChocolate.ScopedStateAttribute"))
            {
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
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
            if (IsOrInheritsFrom(attributeData.AttributeClass, "HotChocolate.LocalStateAttribute"))
            {
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
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
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
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
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
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

        if (parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
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

        if (returnType.Equals(WellKnownTypes.Task) || returnType.Equals(WellKnownTypes.ValueTask))
        {
            return ResolverResultKind.Invalid;
        }

        if (returnType.StartsWith(task) || returnType.StartsWith(valueTask))
        {
            if (returnType.StartsWith(taskEnumerable) || returnType.StartsWith(valueTaskEnumerable))
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

            if (typeDefinition.Equals(WellKnownTypes.EnumerableDefinition, StringComparison.Ordinal))
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
            if (typeName.Equals("HotChocolate.Types.UsePagingAttribute")
                || typeName.Equals("HotChocolate.Types.UseOffsetPagingAttribute"))
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

    public static bool IsOrInheritsFrom(this ITypeSymbol? attributeClass, params string[] fullTypeName)
    {
        var current = attributeClass;

        while (current != null)
        {
            foreach (var typeName in fullTypeName)
            {
                if (current.ToDisplayString() == typeName)
                {
                    return true;
                }
            }

            current = current.BaseType;
        }

        return false;
    }

    public static bool IsOrInheritsFrom(this ITypeSymbol? attributeClass, string fullTypeName)
    {
        var current = attributeClass;

        while (current != null)
        {
            if (current.ToDisplayString() == fullTypeName)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    public static ITypeSymbol? GetReturnType(this ISymbol member)
    {
        ITypeSymbol? returnType;
        if (member is IMethodSymbol method)
        {
            returnType = method.ReturnType;
        }
        else if (member is IPropertySymbol property)
        {
            returnType = property.Type;
        }
        else
        {
            return null;
        }

        if (returnType is INamedTypeSymbol namedTypeSymbol)
        {
            var definitionName = namedTypeSymbol.ConstructedFrom.ToDisplayString();

            if (definitionName.StartsWith("System.Threading.Tasks.Task<")
                || definitionName.StartsWith("System.Threading.Tasks.ValueTask<"))
            {
                return namedTypeSymbol.TypeArguments.FirstOrDefault();
            }
        }

        return returnType;
    }

    public static bool IsConnectionType(this INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        var connectionInterface = compilation.GetTypeByMetadataName("HotChocolate.Types.Pagination.IConnection`1");

        if (connectionInterface == null)
        {
            return false;
        }

        return typeSymbol.AllInterfaces.Any(
            s => SymbolEqualityComparer.Default.Equals(s.OriginalDefinition, connectionInterface));
    }

    public static ITypeSymbol UnwrapTaskOrValueTask(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var originalDefinition = namedType.ConstructedFrom;

            if (originalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<T>"
                || originalDefinition.ToDisplayString() == "System.Threading.Tasks.ValueTask<T>")
            {
                return namedType.TypeArguments[0];
            }
        }

        return typeSymbol;
    }

    /// <summary>
    /// Determines if the method is an accessor (e.g., get_Property, set_Property).
    /// </summary>
    public static bool IsPropertyOrEventAccessor(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.PropertyGet
            || method.MethodKind == MethodKind.PropertySet
            || method.MethodKind == MethodKind.EventAdd
            || method.MethodKind == MethodKind.EventRemove
            || method.MethodKind == MethodKind.EventRaise;
    }

    /// <summary>
    /// Determines if the method is an operator overload (e.g., op_Addition).
    /// </summary>
    public static bool IsOperator(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.UserDefinedOperator
            || method.MethodKind == MethodKind.Conversion;
    }

    public static bool IsConstructor(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.Constructor
            || method.MethodKind == MethodKind.SharedConstructor;
    }

    public static bool IsSpecialMethod(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.Destructor
            || method.MethodKind == MethodKind.LocalFunction
            || method.MethodKind == MethodKind.AnonymousFunction
            || method.MethodKind == MethodKind.DelegateInvoke;
    }

    public static bool IsCompilerGenerated(this IMethodSymbol method)
        => method
            .GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString()
                == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    public static IEnumerable<ISymbol> AllPublicInstanceMembers(this ITypeSymbol type)
    {
        var processed = PooledObjects.GetStringSet();
        var current = type;

        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in current.GetMembers())
            {
                if (member.DeclaredAccessibility == Accessibility.Public
                    && member.Kind is SymbolKind.Property or SymbolKind.Method
                    && !member.IsStatic
                    && !member.IsIgnored()
                    && processed.Add(member.Name))
                {
                    yield return member;
                }
            }

            current = current.BaseType;
        }

        processed.Clear();
        PooledObjects.Return(processed);
    }
}
