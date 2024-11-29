using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Filters;

public sealed class TypeWithAttribute : ISyntaxFilter
{
    private TypeWithAttribute() { }

    public static TypeWithAttribute Instance { get; } = new();

    public bool IsMatch(SyntaxNode node)
        => node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0 };
}
