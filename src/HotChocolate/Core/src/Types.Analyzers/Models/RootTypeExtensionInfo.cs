using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class RootTypeExtensionInfo
    : SyntaxInfo
    , IOutputTypeInfo
{
    private readonly OperationType _operationType;

    public RootTypeExtensionInfo(INamedTypeSymbol type,
        OperationType operationType,
        ClassDeclarationSyntax classDeclarationSyntax,
        ImmutableArray<Resolver> resolvers)
    {
        _operationType = operationType;
        Name = type.ToFullyQualified();
        Type = type;
        ClassDeclarationSyntax = classDeclarationSyntax;
        Resolvers = resolvers;
    }

    public string Name { get; }

    public bool IsRootType => true;

    public OperationType OperationType => _operationType;

    public INamedTypeSymbol Type { get; }

    public INamedTypeSymbol? RuntimeType => null;

    public ClassDeclarationSyntax ClassDeclarationSyntax { get; }

    public ImmutableArray<Resolver> Resolvers { get; }

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
