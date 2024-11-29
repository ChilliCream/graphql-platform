using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class DataLoaderInfo : SyntaxInfo
{
    private readonly string[] _lookups;

    public DataLoaderInfo(
        AttributeSyntax attributeSyntax,
        IMethodSymbol attributeSymbol,
        IMethodSymbol methodSymbol,
        MethodDeclarationSyntax methodSyntax)
    {
        Validate(methodSymbol, methodSyntax);

        AttributeSyntax = attributeSyntax;
        AttributeSymbol = attributeSymbol;
        MethodSymbol = methodSymbol;
        MethodSyntax = methodSyntax;

        var attribute = methodSymbol.GetDataLoaderAttribute();
        _lookups = attribute.GetLookups();
        var declaringType = methodSymbol.ContainingType;

        NameWithoutSuffix = GetDataLoaderName(methodSymbol.Name, attribute);
        Name = NameWithoutSuffix + "DataLoader";
        InterfaceName = $"I{Name}";
        Namespace = methodSymbol.ContainingNamespace.ToDisplayString();
        FullName = $"{Namespace}.{Name}";
        InterfaceFullName = $"{Namespace}.{InterfaceName}";
        IsScoped = attribute.IsScoped();
        IsPublic = attribute.IsPublic();
        IsInterfacePublic = attribute.IsInterfacePublic();
        MethodName = methodSymbol.Name;
        KeyParameter = MethodSymbol.Parameters[0];
        ContainingType = declaringType.ToDisplayString();
        Parameters = CreateParameters(methodSymbol);
        Groups = methodSymbol.GetDataLoaderGroupKeys();
    }

    public string Name { get; }

    public string NameWithoutSuffix { get; }

    public string FullName { get; }

    public ImmutableHashSet<string> Groups { get; }

    public string Namespace { get; }

    public string InterfaceName { get; }

    public string InterfaceFullName { get; }

    public string ContainingType { get; }

    public string MethodName { get; }

    public bool? IsScoped { get; }

    public bool? IsPublic { get; }

    public bool? IsInterfacePublic { get; }

    public AttributeSyntax AttributeSyntax { get; }

    public IMethodSymbol AttributeSymbol { get; }

    public IMethodSymbol MethodSymbol { get; }

    public MethodDeclarationSyntax MethodSyntax { get; }

    public IParameterSymbol KeyParameter { get; }

    public ImmutableArray<DataLoaderParameterInfo> Parameters { get; }

    public override string OrderByKey => FullName;

    public ImmutableArray<CacheLookup> GetLookups(ITypeSymbol keyType, ITypeSymbol valueType)
    {
        if (_lookups.Length > 0)
        {
            var builder = ImmutableArray.CreateBuilder<CacheLookup>();

            foreach (var lookup in _lookups)
            {
                foreach (var method in MethodSymbol.ContainingType.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.Name == lookup))
                {
                    if (method.Parameters.Length == 1
                        && method.Parameters[0].Type.Equals(valueType, SymbolEqualityComparer.Default)
                        && method.ReturnType.Equals(keyType, SymbolEqualityComparer.Default))
                    {
                        builder.Add(new CacheLookup(method));
                    }

                    if (method.Parameters.Length == 1
                        && IsKeyValuePair(method.ReturnType, keyType, valueType))
                    {
                        builder.Add(new CacheLookup(method, isTransform: true));
                    }
                }
            }

            return builder.ToImmutable();
        }

        return ImmutableArray<CacheLookup>.Empty;
    }

    private void Validate(
        IMethodSymbol methodSymbol,
        MethodDeclarationSyntax methodSyntax)
    {
        if (methodSymbol.Parameters.Length == 0)
        {
            AddDiagnostic(
                Diagnostic.Create(
                    Errors.KeyParameterMissing,
                    Location.Create(
                        methodSyntax.SyntaxTree,
                        methodSyntax.ParameterList.Span)));
        }

        if (methodSymbol.DeclaredAccessibility is
            not Accessibility.Public and
            not Accessibility.Internal and
            not Accessibility.ProtectedAndInternal)
        {
            AddDiagnostic(
                Diagnostic.Create(
                    Errors.MethodAccessModifierInvalid,
                    Location.Create(
                        methodSyntax.SyntaxTree,
                        methodSyntax.Modifiers.Span)));
        }

        if (methodSymbol.IsGenericMethod)
        {
            AddDiagnostic(
                Diagnostic.Create(
                    Errors.DataLoaderCannotBeGeneric,
                    Location.Create(
                        methodSyntax.SyntaxTree,
                        methodSyntax.Modifiers.Span)));
        }
    }

    private static ImmutableArray<DataLoaderParameterInfo> CreateParameters(IMethodSymbol method)
    {
        var builder = ImmutableArray.CreateBuilder<DataLoaderParameterInfo>();
        builder.Add(new DataLoaderParameterInfo("keys", method.Parameters[0], DataLoaderParameterKind.Key));

        for (var i = 1; i < method.Parameters.Length; i++)
        {
            var parameter = method.Parameters[i];

            // first we check if the parameter is a cancellation token.
            if (IsCancellationToken(parameter))
            {
                builder.Add(
                    new DataLoaderParameterInfo(
                        "ct",
                        parameter,
                        DataLoaderParameterKind.CancellationToken));
                continue;
            }

            // check for well-known state
            if (IsSelectorBuilder(parameter))
            {
                builder.Add(
                    new DataLoaderParameterInfo(
                        $"p{i}",
                        parameter,
                        DataLoaderParameterKind.SelectorBuilder,
                        WellKnownTypes.SelectorBuilder));
                continue;
            }

            // check for well-known state
            if (IsPredicateBuilder(parameter))
            {
                builder.Add(
                    new DataLoaderParameterInfo(
                        $"p{i}",
                        parameter,
                        DataLoaderParameterKind.PredicateBuilder,
                        WellKnownTypes.PredicateBuilder));
                continue;
            }

            if (IsPagingArguments(parameter))
            {
                builder.Add(
                    new DataLoaderParameterInfo(
                        $"p{i}",
                        parameter,
                        DataLoaderParameterKind.PagingArguments,
                        WellKnownTypes.PagingArguments));
                continue;
            }

            var stateKey = parameter.GetDataLoaderStateKey();

            // if the parameter is annotated as a state attribute we will get here a state key.
            if (stateKey is not null)
            {
                builder.Add(
                    new DataLoaderParameterInfo(
                        $"p{i}",
                        parameter,
                        DataLoaderParameterKind.ContextData,
                        stateKey));
                continue;
            }

            // lastly if we land here we assume that the parameter is a service.
            builder.Add(
                new DataLoaderParameterInfo(
                    $"p{i}",
                    parameter,
                    DataLoaderParameterKind.Service));
        }

        return builder.ToImmutable();
    }

    private static bool IsCancellationToken(IParameterSymbol parameter)
    {
        var typeName = parameter.Type.ToDisplayString();
        return string.Equals(typeName, WellKnownTypes.CancellationToken, StringComparison.Ordinal);
    }

    private static bool IsSelectorBuilder(IParameterSymbol parameter)
    {
        var typeName = parameter.Type.ToDisplayString();
        return string.Equals(typeName, WellKnownTypes.SelectorBuilder, StringComparison.Ordinal);
    }

    private static bool IsPredicateBuilder(IParameterSymbol parameter)
    {
        var typeName = parameter.Type.ToDisplayString();
        return string.Equals(typeName, WellKnownTypes.PredicateBuilder, StringComparison.Ordinal);
    }

    private static bool IsPagingArguments(IParameterSymbol parameter)
    {
        var typeName = parameter.Type.ToDisplayString();
        return string.Equals(typeName, WellKnownTypes.PagingArguments, StringComparison.Ordinal);
    }

    public static bool IsKeyValuePair(ITypeSymbol returnTypeSymbol, ITypeSymbol keyType, ITypeSymbol valueType)
    {
        if (returnTypeSymbol is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol.IsGenericType
            && namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.None
            && namedTypeSymbol.ConstructedFrom.ToDisplayString().StartsWith(WellKnownTypes.KeyValuePair)
            && keyType.Equals(namedTypeSymbol.TypeArguments[0], SymbolEqualityComparer.Default)
            && valueType.Equals(namedTypeSymbol.TypeArguments[1], SymbolEqualityComparer.Default))
        {
            return true;
        }

        return false;
    }

    public override bool Equals(object? obj)
        => obj is DataLoaderInfo other && Equals(other);

    public override bool Equals(SyntaxInfo obj)
        => obj is DataLoaderInfo other && Equals(other);

    private bool Equals(DataLoaderInfo other)
        => AttributeSyntax.IsEquivalentTo(other.AttributeSyntax)
            && MethodSyntax.IsEquivalentTo(other.MethodSyntax)
            && Groups.SequenceEqual(other.Groups, StringComparer.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(AttributeSyntax, MethodSyntax);

    private static string GetDataLoaderName(string name, AttributeData attribute)
    {
        if (attribute.TryGetName(out var s))
        {
            return s;
        }

        if (name.StartsWith("Get"))
        {
            name = name.Substring(3);
        }

        if (name.EndsWith("Async"))
        {
            name = name.Substring(0, name.Length - 5);
        }

        return name.EndsWith("DataLoader")
            ? name.Substring(0, name.Length - 10)
            : name;
    }
}
