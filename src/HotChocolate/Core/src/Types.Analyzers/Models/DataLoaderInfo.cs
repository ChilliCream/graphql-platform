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
