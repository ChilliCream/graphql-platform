using HotChocolate.Types.Analyzers.Helpers;
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

        var attribute = methodSymbol.GetDataLoaderAttribute();

        Name = GetDataLoaderName(methodSymbol.Name, attribute);
        InterfaceName = $"I{Name}";
        Namespace = methodSymbol.ContainingNamespace.ToDisplayString();
        FullName = $"{Namespace}.{Name}";
        InterfaceFullName = $"{Namespace}.{InterfaceName}";
        IsScoped = attribute.IsScoped();
        IsPublic = attribute.IsPublic();
        IsInterfacePublic = attribute.IsInterfacePublic();
        MethodName = methodSymbol.Name;

        var type = methodSymbol.ContainingType;
        ContainingType = type.ToDisplayString();
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

        return other is DataLoaderInfo info && Equals(info);
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
