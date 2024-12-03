using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class InterfaceTypeExtensionInfo(
    INamedTypeSymbol type,
    INamedTypeSymbol runtimeType,
    ClassDeclarationSyntax classDeclarationSyntax,
    ImmutableArray<Resolver> resolvers)
    : SyntaxInfo
    , IOutputTypeInfo
{
    public string Name { get; } = type.ToFullyQualified();

    public INamedTypeSymbol Type { get; } = type;

    public INamedTypeSymbol RuntimeType { get; } = runtimeType;

    public ClassDeclarationSyntax ClassDeclarationSyntax { get; } = classDeclarationSyntax;

    public ImmutableArray<Resolver> Resolvers { get; } = resolvers;

    public override string OrderByKey => Name;

    public override bool Equals(object? obj)
        => obj is ObjectTypeExtensionInfo other && Equals(other);

    public override bool Equals(SyntaxInfo obj)
        => obj is ObjectTypeExtensionInfo other && Equals(other);

    private bool Equals(ObjectTypeExtensionInfo other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal) &&
            ClassDeclarationSyntax.SyntaxTree.IsEquivalentTo(
                other.ClassDeclarationSyntax.SyntaxTree);

    public override int GetHashCode()
        => HashCode.Combine(Name, ClassDeclarationSyntax);
}
