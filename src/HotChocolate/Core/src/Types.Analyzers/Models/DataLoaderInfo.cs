using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class DataLoaderInfo : SyntaxInfo
{
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
        var lookups = Array.Empty<string>(); // attribute.GetLookups();
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

        if (lookups.Length > 0)
        {
            var builder = ImmutableArray.CreateBuilder<IMethodSymbol>();

            foreach (var lookup in lookups)
            {
                foreach (var method in declaringType.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.Name == lookup))
                {
                    if (method.Name.Equals(lookup, StringComparison.Ordinal)
                        && method.Parameters.Length == 1
                        && method.ReturnType.Equals(KeyParameter.Type, SymbolEqualityComparer.Default))
                    {
                        builder.Add(method);
                    }
                }
            }

            Lookups = builder.ToImmutable();
        }
        else
        {
            Lookups = ImmutableArray<IMethodSymbol>.Empty;
        }
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

    public ImmutableArray<IMethodSymbol> Lookups { get; }

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
