using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ObjectTypeExtensionInfo(
    INamedTypeSymbol type,
    INamedTypeSymbol runtimeType,
    IMethodSymbol? nodeResolver,
    ImmutableArray<ISymbol> members,
    ImmutableArray<Diagnostic> diagnostics,
    ClassDeclarationSyntax classDeclarationSyntax)
    : ISyntaxInfo
{
    public string Name { get; } = type.ToFullyQualified();

    public INamedTypeSymbol Type { get; } = type;

    public INamedTypeSymbol RuntimeType { get; } = runtimeType;

    public IMethodSymbol? NodeResolver { get; } = nodeResolver;

    public ImmutableArray<ISymbol> Members { get; } = members;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;

    public ClassDeclarationSyntax ClassDeclarationSyntax { get; } = classDeclarationSyntax;

    public override bool Equals(object? obj)
        => obj is ObjectTypeExtensionInfo other && Equals(other);

    public bool Equals(ISyntaxInfo obj)
        => obj is ObjectTypeExtensionInfo other && Equals(other);

    private bool Equals(ObjectTypeExtensionInfo other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal)
            && ClassDeclarationSyntax.SyntaxTree.IsEquivalentTo(
                other.ClassDeclarationSyntax.SyntaxTree);

    public override int GetHashCode()
        => HashCode.Combine(Name, ClassDeclarationSyntax);
}
