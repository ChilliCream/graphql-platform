using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class DataLoaderInfo : ISyntaxInfo, IEquatable<DataLoaderInfo>
{
    public DataLoaderInfo(
        AttributeSyntax attributeSyntax,
        IMethodSymbol attributeSymbol,
        IMethodSymbol methodSymbol,
        MethodDeclarationSyntax methodSyntax)
    {
        AttributeSyntax = attributeSyntax;
        AttributeSymbol = attributeSymbol;
        MethodSymbol = methodSymbol;
        MethodSyntax = methodSyntax;
    }

    public AttributeSyntax AttributeSyntax { get; }

    public IMethodSymbol AttributeSymbol { get; }

    public IMethodSymbol MethodSymbol { get; }

    public MethodDeclarationSyntax MethodSyntax { get; }

    public bool Equals(DataLoaderInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return AttributeSyntax.Equals(other.AttributeSyntax) &&
            MethodSyntax.Equals(other.MethodSyntax);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj)
            || obj is DataLoaderInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = AttributeSyntax.GetHashCode();
            hashCode = (hashCode * 397) ^ MethodSyntax.GetHashCode();
            return hashCode;
        }
    }
}
