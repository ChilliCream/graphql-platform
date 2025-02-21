using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public class ConnectionTypeInfo(
    INamedTypeSymbol type,
    ClassDeclarationSyntax classDeclaration)
    : SyntaxInfo
{
    public string TypeName => Type.Name;

    public INamedTypeSymbol Type { get; } = type;

    public bool IsGeneric => Type.IsGenericType;

    public ClassDeclarationSyntax ClassDeclaration { get; } = classDeclaration;

    public override string OrderByKey => TypeName;

    public override bool Equals(SyntaxInfo obj)
        => obj is ConnectionTypeInfo other && Equals(other);

    private bool Equals(ConnectionTypeInfo other)
        => TypeName.Equals(other.TypeName, StringComparison.Ordinal)
            && IsGeneric == other.IsGeneric
            && ClassDeclaration.IsEquivalentTo(other.ClassDeclaration);

    public override int GetHashCode()
        => HashCode.Combine(TypeName, IsGeneric, ClassDeclaration);
}
