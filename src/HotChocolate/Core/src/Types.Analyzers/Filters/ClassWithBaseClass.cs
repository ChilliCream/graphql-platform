using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Filters;

public sealed class ClassWithBaseClass : ISyntaxFilter
{
    private ClassWithBaseClass() { }

    public static ClassWithBaseClass Instance { get; } = new();

    public bool IsMatch(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 };
}
