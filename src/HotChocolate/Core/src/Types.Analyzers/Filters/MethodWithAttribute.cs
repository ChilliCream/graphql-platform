using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Filters;

public sealed class MethodWithAttribute : ISyntaxFilter
{
    private MethodWithAttribute() { }

    public static MethodWithAttribute Instance { get; } = new();

    public bool IsMatch(SyntaxNode node)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };
}
