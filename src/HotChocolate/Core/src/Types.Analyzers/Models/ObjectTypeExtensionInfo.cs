using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ObjectTypeExtensionInfo : ISyntaxInfo, IEquatable<ObjectTypeExtensionInfo>
{
    public ObjectTypeExtensionInfo(
        INamedTypeSymbol type,
        INamedTypeSymbol runtimeType,
        IMethodSymbol? nodeResolver,
        ImmutableArray<ISymbol> members,
        ImmutableArray<Diagnostic> diagnostics,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        Name = type.ToFullyQualified();
        NodeResolver = nodeResolver;
        Members = members;
        Type = type;
        RuntimeType = runtimeType;
        Diagnostics = diagnostics;
        ClassDeclarationSyntax = classDeclarationSyntax;
    }

    public string Name { get; }

    public INamedTypeSymbol Type { get; }

    public INamedTypeSymbol RuntimeType { get; }

    public IMethodSymbol? NodeResolver { get; }

    public ImmutableArray<ISymbol> Members { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public ClassDeclarationSyntax ClassDeclarationSyntax { get; }

    public bool Equals(ObjectTypeExtensionInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name &&
            ClassDeclarationSyntax.SyntaxTree.IsEquivalentTo(
                other.ClassDeclarationSyntax.SyntaxTree);
    }

    public bool Equals(ISyntaxInfo other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is ObjectTypeExtensionInfo info && Equals(info);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ObjectTypeExtensionInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Name.GetHashCode() * 397) ^ ClassDeclarationSyntax.GetHashCode();
        }
    }

    public static bool operator ==(ObjectTypeExtensionInfo? left, ObjectTypeExtensionInfo? right)
        => Equals(left, right);

    public static bool operator !=(ObjectTypeExtensionInfo? left, ObjectTypeExtensionInfo? right)
        => !Equals(left, right);
}
