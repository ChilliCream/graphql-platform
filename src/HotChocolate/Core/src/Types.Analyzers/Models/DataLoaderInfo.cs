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

        Name = GetDataLoaderName(methodSymbol.Name, attribute);
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

    public string Name { get; }

    public string FullName { get; }

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
            && MethodSyntax.IsEquivalentTo(other.MethodSyntax);

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

        if (name.EndsWith("DataLoader"))
        {
            return name;
        }

        return name + "DataLoader";
    }
}
